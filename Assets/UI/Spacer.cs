using UnityEngine.UIElements;

[UIWidget("spacer")]
public class Spacer : VisualElement
{
    public Spacer()
    {
        style.flexGrow = 1;
    }
}