using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class UITinter : MonoBehaviour
{
    [SerializeField]
    Color[] m_TintColors;
    Image m_Image;
    private void Awake()
    {
        m_Image = GetComponent<Image>();
    }

    /// <summary>
    /// ǰ������ɫ�л�
    /// </summary>
    /// <param name="firstTwoColors">true�����һ��</param>
    public void SetToColor(bool firstTwoColors)
    {
        int colorInt = firstTwoColors ? 1 : 0;
        if (colorInt >= m_TintColors.Length)
            return;
        m_Image.color = m_TintColors[colorInt];
    }

    public void SetToColor(int colorInt)
    {
        if (colorInt >= m_TintColors.Length)
            return;
        m_Image.color = m_TintColors[colorInt];
    }
}
