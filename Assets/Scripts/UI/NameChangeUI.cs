
using TMPro;

public class NameChangeUI : UIPanelBase
{
    public TMP_InputField inputName;
    //�༭��������ؼ�,�������������ʱ����.
    public void OnEndNameEdit()
    {
        Manager.SetLocalPlayerName(inputName.text);
    }
}
