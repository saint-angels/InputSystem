using UnityEditor;
using UnityEngine.InputSystem;

public class ISXProcessorAutoWorldSpaceWindow : EditorWindow
{
    private static InputAction m_action;

    private void OnGUI()
    {
    }

    [MenuItem("QA Tools/Input Test/Processor Test: Auto World Space", true, 11)]
    private static bool CheckOpenTestWindow()
    {
        return false;
    }

    [MenuItem("QA Tools/Input Test/Processor Test: Auto World Space", false, 11)]
    private static void OpenTestWindow()
    {
        var window = GetWindow<ISXProcessorAutoWorldSpaceWindow>();
        window.Show();
    }
}