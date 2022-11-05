using UnityEngine;
using UnityEngine.InputSystem;

namespace CircleGesture
{
    public class Root : MonoBehaviour, GestureControls.IDefaultActionMapActions
    {
        private GestureControls controls;

        private void Start()
        {
            // InputSystem.RegisterInteraction<CircleInteraction>();

            if (controls == null)
            {
                controls = new GestureControls();
                controls.DefaultActionMap.SetCallbacks(this);
            }

            controls.DefaultActionMap.Enable();
        }

        public void OnCircleAction(InputAction.CallbackContext context)
        {
            if (context.performed) Debug.Log("Circle performed!!");
        }
    }
}