using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Remoting.Contexts;
using Mono.Cecil.Cil;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Scripting;
#if UNITY_EDITOR
using UnityEngine.InputSystem.Editor;
#endif

namespace UnityEngine.InputSystem.Interactions
{
    /// <summary>
    /// Performs the action if the control is pressed and held for at least the
    /// set duration (which defaults to <see cref="InputSettings.defaultHoldTime"/>).
    /// </summary>
    [DisplayName("Circle")]
    public class CircleInteraction : IInputInteraction<Vector2>
    {
        public float duration;

        /// <summary>
        /// Magnitude threshold that must be crossed by an actuated control for the control to
        /// be considered pressed.
        /// </summary>
        /// <remarks>
        /// If this is less than or equal to 0 (the default), <see cref="InputSettings.defaultButtonPressPoint"/> is used instead.
        /// </remarks>
        /// <seealso cref="InputControl.EvaluateMagnitude()"/>
        public float pressPoint;

        private float pressPointOrDefault =>
            pressPoint > 0.0 ? pressPoint : ButtonControl.s_GlobalDefaultButtonPressPoint;

        private double m_TimePressed;

        private InputActionPhase actionPhase;
        private List<Vector2> gesturePoints;

        /// <inheritdoc />
        public void Process(ref InputInteractionContext context)
        {
            if (actionPhase != context.phase)
            {
                Debug.Log($"context phase {actionPhase} -> {context.phase}");
                actionPhase = context.phase;
            }
            // Debug.Log(context.control.ReadValueAsObject());

            if (context.timerHasExpired)
            {
                context.Canceled();
                return;
            }

            switch (context.phase)
            {
                case InputActionPhase.Waiting:
                    // if (context.ControlIsActuated(pressPointOrDefault))
                {
                    m_TimePressed = context.time;

                    context.Started();
                    context.SetTimeout(duration);

                    gesturePoints = new();
                }

                    break;

                case InputActionPhase.Started:
                    var newGesturePoint = (Vector2)context.control.ReadValueAsObject();
                    gesturePoints.Add(newGesturePoint);
                    // Debug.Log(gesturePoints.Count);

                    //If we don't have enough points, exit
                    if (gesturePoints.Count < 2)
                    {
                        return;
                    }

                    int inflections = 0;
                    for (int i = 2; i < (gesturePoints.Count - 1); i++)
                    {
                        float currentDx = dx(gesturePoints[i], gesturePoints[i - 1]);
                        float currentDy = dy(gesturePoints[i], gesturePoints[i - 1]);
                        float prevDx = dx(gesturePoints[i - 1], gesturePoints[i - 2]);
                        float prevDy = dy(gesturePoints[i - 1], gesturePoints[i - 2]);

                        bool signChangedX = Sign(currentDx) != Sign(prevDx) && currentDx != 0 && prevDx != 0;
                        bool signChangedY = Sign(currentDy) != Sign(prevDy) && currentDy != 0 && prevDy != 0;
                        if (signChangedX || signChangedY)
                        {
                            Debug.Log($"inflection dx:{currentDx} dy:{currentDy} prevDx:{prevDx} prevDy{prevDy}");
                            inflections++;
                        }
                    }

                    if (0 < inflections)
                    {
                        // Debug.Log($"inflections count: {inflections}");
                        if (inflections > 5)
                        {
                            Debug.LogError(@"Excessive inflections");
                            context.Canceled();
                            return;
                        }
                    }


                    Rect circleRect = BoundingRect(gesturePoints);

                    Vector2 center = circleRect.center;
                    float distance = AngleBetweenPoints(gesturePoints[0], gesturePoints[1], center);
                    for (int i = 1; i < gesturePoints.Count - 1; i++)
                    {
                        distance += AngleBetweenPoints(gesturePoints[i], gesturePoints[i + 1], center);
                    }
                    // Debug.Log(distance);

                    float transitTolerance = distance - 2 * Mathf.PI;

                    if (transitTolerance < 0.0f) // fell short of 2 PI
                    {
                         // under 45
                        bool isTooFarFromCircle = transitTolerance < -(Mathf.PI / 4.0f);
                        if (isTooFarFromCircle)
                        {
                            return;
                        }
                    }

                    if (transitTolerance > Mathf.PI) // additional 180 degrees
                    {
                        // Debug.Log("too long");
                        return;
                    }

                    context.Performed();
                    break;

                case InputActionPhase.Performed:
                    if (!context.ControlIsActuated(pressPointOrDefault))
                        context.Canceled();
                    break;
            }

            float AngleBetweenPoints(Vector2 point1, Vector2 point2, Vector2 center)
            {
                bool pointOnCenter = point1 == center || point2 == center;
                if (pointOnCenter)
                {
                    return 0;
                }
                Vector2 localPoint0 = PointWithOrigin(point1, center);
                Vector2 localPoint1 = PointWithOrigin(point2, center);
                float dotProduct = Vector2.Dot(localPoint0, localPoint1);
                float dotProductNormalized = dotProduct / (localPoint0.magnitude * localPoint1.magnitude);
                //Acos returns angle between vectors in radians
                float acos = Mathf.Acos(dotProductNormalized);
                float resultAngle = Mathf.Abs(acos);
                if (float.IsNaN(resultAngle))
                {
                    Debug.Log($"angle between points is NAN");
                    return 0;
                }

                return resultAngle;
            }

            Vector2 PointWithOrigin(Vector2 pt, Vector2 origin)
            {
                return new Vector2(pt.x - origin.x, pt.y - origin.y);
            }

            int Sign(float x)
            {
                return (x < 0.0f) ? (-1) : 1;
            }

            float dx(Vector2 p1, Vector2 p2)
            {
                return p2.x - p1.x;
            }

            float dy(Vector2 p1, Vector2 p2)
            {
                return p2.y - p1.y;
            }

            Rect BoundingRect(List<Vector2> points)
            {
                float minX = Mathf.Infinity;
                float maxX = Mathf.NegativeInfinity;
                float minY = Mathf.Infinity;
                float maxY = Mathf.NegativeInfinity;

                for (int i = 0; i < points.Count; i++)
                {
                    if (points[i].x < minX)
                    {
                        minX = points[i].x;
                    }

                    if (maxX < points[i].x)
                    {
                        maxX = points[i].x;
                    }

                    if (points[i].y < minY)
                    {
                        minY = points[i].y;
                    }

                    if (maxY < points[i].y)
                    {
                        maxY = points[i].y;
                    }
                }

                return Rect.MinMaxRect(minX, minY, maxX, maxY);
            }
        }

        /// <inheritdoc />
        public void Reset()
        {
            m_TimePressed = 0;
            Debug.Log($"context phase {actionPhase} -> {InputActionPhase.Waiting}");
            actionPhase = InputActionPhase.Waiting;
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// UI that is displayed when editing <see cref="HoldInteraction"/> in the editor.
    /// </summary>
    internal class CircleInteractionEditor : InputParameterEditor<CircleInteraction>
    {
        protected override void OnEnable()
        {
            m_PressPointSetting.Initialize("Press Point",
                "Float value that an axis control has to cross for it to be considered pressed.",
                "Default Button Press Point",
                () => target.pressPoint, v => target.pressPoint = v,
                () => ButtonControl.s_GlobalDefaultButtonPressPoint);
            m_DurationSetting.Initialize("Gesture time",
                "Time in which the gesture has to be performed after it's start.",
                "Default Duration Time",
                () => target.duration, x => target.duration = x, () => 1f);
        }

        public override void OnGUI()
        {
            m_PressPointSetting.OnGUI();
            m_DurationSetting.OnGUI();
        }

        private CustomOrDefaultSetting m_PressPointSetting;
        private CustomOrDefaultSetting m_DurationSetting;
    }
#endif
}