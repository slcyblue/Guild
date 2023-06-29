using Beebyte.Obfuscator;
using UnityEngine;
using UnityEngine.UI;

public class GuildPassiveSlot : MonoBehaviour
{

    #region Variables
    
    [SerializeField] GameObject learnIconPrefab = null;
    
    [SerializeField] RectTransform rect = null;
    [SerializeField] Text txtName = null;
    [SerializeField] Text txtLv = null;
    [SerializeField] Image skillImage = null;
    [SerializeField] Image frameImage = null;
    [SerializeField] GameObject lockObject = null;

    [HideInInspector] public int gPassiveId { get; private set; } = 0;
    [HideInInspector] public int gPassiveLv { get; private set; } = 0;
    [HideInInspector] public int Index { get; private set; } = 0;
    [HideInInspector] public Sprite frameSprite = null;
    
    GameObject alarmObject = null;
    public GuildPassiveCSV csv { get; private set; } = null;
  

    public delegate void OnClickEvent(GuildPassiveSlot slot);
    private OnClickEvent clickCallback = null;

    #endregion

    #region Init
    public void Init(int idx, int _skillId)
    {
        gPassiveId = _skillId;
        Index = idx;

        csv = CSVParser.GetGuildPassiveCSV(gPassiveId);

        txtName.text = LocalizationSystem.GetLocalizedText( csv.gPassiveName);
        
        skillImage.sprite = ResourceManager.GetInstance().GetSkillImage(csv.gPassiveImageName);
        
        gameObject.SetActive(true);
    }

    public void SetPosition(float xPos, float yPos)
    {
        rect.anchoredPosition = new Vector2(xPos, yPos);
    }

    public void AddListener(OnClickEvent action)
    {
        clickCallback = action;
    }

    #endregion


    #region UI
    /// <summary>
    /// ���Ŵ����� ��ϵ� ����нú� �����͸� ���� �нú극���� 0�� ���(����� ���� ���) ���ó��.
    /// </summary>
    public void UpdateLevelInfo()
    {
        if (GuildManager.instance.myGuildInfo.guildPassive[gPassiveId].gPassiveLv != 0)
        {
            gPassiveLv = GuildManager.instance.myGuildInfo.guildPassive[gPassiveId].gPassiveLv;
            txtLv.text = $"Lv.{gPassiveLv}";
                
            lockObject.SetActive(false);
            if (alarmObject != null) alarmObject.SetActive(false);
        }
        else
        {
            txtLv.text = "";
            lockObject.SetActive(true);
            UpdateLearnInfo();
        }

        string frameName = csv.frameImage;
        frameSprite = ResourceManager.GetInstance().GetSkillFrame("GpassivFrameN");
        //frameSprite = ResourceManager.GetInstance().GetSkillFrame("csv.frameImage");
        frameImage.sprite = frameSprite;   
    }

    //��������� ������ �˸��� ��� �� �ְ� ����.
    public void UpdateLearnInfo()
    {
        bool isOn = CanLearnPassive(gPassiveId);
        if (!isOn)
        {
            if (alarmObject != null) alarmObject.SetActive(false);
        }
        else
        {
            if (alarmObject == null)
            {
                alarmObject = Instantiate(learnIconPrefab, transform);
            }

            alarmObject.SetActive(true);
        }
    }

    /// <summary>
    /// ����нú�Id�� LV�� 0�� �ƴ� ���(�нú긦 ����� �ʾ��� ���), UserData�� ��ȭ���� CSV������ �䱸 ��ȭ���� ���� bool ���� ��ȯ�ϴ� �Լ�.
    /// </summary>
    /// <param name="passiveID"></param>
    /// <returns></returns>
    public bool CanLearnPassive(int passiveID)
    {

        if (GuildManager.instance.GetPassiveLv(passiveID) != 0) return false;

        GuildPassiveCSV csv = CSVParser.GetGuildPassiveCSV(passiveID);

        if (UserData.instance.GetCount(csv.needItemType, csv.openItemRefId) < csv.openItemCnt) return false;

        else return true;
    }
    #endregion


    #region Action

    [SkipRename]
    public void ClickSkillIcon()
    {
        SoundManager.instance.PlaySFX(SoundEnum.uiClickBtn);
        clickCallback?.Invoke(this);
    }

    #endregion

}
