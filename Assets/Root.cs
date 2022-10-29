using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

public class Root : MonoBehaviour, GestureControls.IDefaultActionMapActions
{
    private GestureControls controls;
    
    
    // Start is called before the first frame update
    void Start()
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
        Debug.Log("Circle action!");
        
    }
}
