
using TMPro;

public class NameChangeUI : UIPanelBase
{
    public TMP_InputField inputName;
    //编辑器绑定输入控件,在名字输入完成时触发.
    public void OnEndNameEdit()
    {
        Manager.SetLocalPlayerName(inputName.text);
    }
}
