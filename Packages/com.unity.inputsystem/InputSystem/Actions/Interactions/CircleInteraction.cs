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
                        float deltx = dx(gesturePoints[i], gesturePoints[i - 1]);
                        float delty = dy(gesturePoints[i], gesturePoints[i - 1]);
                        float px = dx(gesturePoints[i - 1], gesturePoints[i - 2]);
                        float py = dy(gesturePoints[i - 1], gesturePoints[i - 2]);

                        if ((Sign(deltx) != Sign(px)) ||
                            (Sign(delty) != Sign(py)))
                            inflections++;
                    }

                    if (inflections > 5)
                    {
                        Debug.LogError(@"Excessive inflections");
                        context.Canceled();
                        return;
                    }

                {
                }
                    // If we've reached our hold time threshold, perform the hold.
                    // We do this regardless of what state the control changed to.
                    // if (context.time - m_TimePressed >= duration)
                    // {
                    //     context.PerformedAndStayPerformed();
                    // }
                    break;

                case InputActionPhase.Performed:
                    if (!context.ControlIsActuated(pressPointOrDefault))
                        context.Canceled();
                    break;
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