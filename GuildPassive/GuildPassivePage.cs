using Beebyte.Obfuscator;
using CodeStage.AntiCheat.ObscuredTypes;
using ESA.Battle.Skill;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class GuildPassivePage : UIPage
{
    #region Variable
    [Header("List")]
    [SerializeField] GameObject iconPrefab = null;
    [SerializeField] RectTransform passiveListParent = null;
    public RectMask2D particleMask = null;

    [SerializeField] float INIT_X = 0f;
    [SerializeField] float INIT_Y = 0f;
    [SerializeField] float WIDTH = 0f;
    [SerializeField] float HEIGHT = 0f;

    [Header("Resource")]
    [SerializeField] Text txtGCoinCnt = null;
    [SerializeField] Image imgGCoinIcon = null;


    [Header("SkillInfo")]
    [SerializeField] Text txtPassiveName = null;
    [SerializeField] Text txtDescription = null;
    [SerializeField] Image passiveImage = null;
    //[SerializeField] Image passiveFrameImage = null;
    [SerializeField] Text txtLevel = null;
    

    [Header("Buttons")]
    [SerializeField] Button btnUpgrade = null;
    [SerializeField] Text txtUpgradePrice = null;
    [SerializeField] Image payUpgradeItemImage = null;

    [SerializeField] Button btnOpen = null;
    [SerializeField] Text txtOpenPrice = null;
    [SerializeField] Image payOpenItemImage = null;

    [SerializeField] Sprite blockedSprite = null;
    [SerializeField] Sprite originSprite = null;
    [SerializeField] public GameObject loadingImg = null;

    GuildPassiveCSV csv = null;
    GuildPassiveLvCSV levelCsv = null;

    GuildPassiveSlot crrFocusSlot = null;
    ObscuredInt crrFocusSkillLevel = 0;
    GuildPassiveSlot[] guildPassiveList = null;
    private bool HasSkill = false;

    Dictionary<int, GuildPassive> newPassive;
    ObscuredInt price = 0;

    Action<string> onSuccess = null;
    Action<string> onFailed = null;

    Source src;

    public GuildHomePage mgr;
    #endregion

    public enum UseType
    {
        None,
        Open,
        Upgrade
    }

    UseType type;

    #region Init
    public override void Initialize()
    {
        iconPrefab.gameObject.SetActive(false);
        onSuccess = onSuccessAction;
        onFailed = onFailedAction;

        List<int> list = new List<int>(CSVParser.guildPassives.Keys);
        int length = list.Count;
        guildPassiveList = new GuildPassiveSlot[length];

        float xPos = INIT_X;
        float yPos = INIT_Y;
        for (int i = 0; i < length; i++)
        {
            GameObject newObj = Instantiate(iconPrefab, passiveListParent);
            guildPassiveList[i] = newObj.GetComponent<GuildPassiveSlot>();

            guildPassiveList[i].Init(i, list[i]);
            //guildPassiveList[i].SetParticleMaskingParent(particleMask);
            guildPassiveList[i].AddListener(ShowSkillInfo);
            guildPassiveList[i].SetPosition(xPos, yPos);

            if (i % 4 == 3)
            {
                yPos -= HEIGHT;
                xPos = INIT_X;
            }
            else
            {
                xPos += WIDTH;
            }
        }

        passiveListParent.sizeDelta = new Vector3(0, (-1) * yPos + HEIGHT, 0);
    }

    public override void OpenPage()
    {
        if (crrFocusSlot == null) crrFocusSlot = guildPassiveList[0];

        int length = guildPassiveList.Length;

        for (int i = 0; i < length; i++)
        {
            guildPassiveList[i].UpdateLevelInfo();
        }

        ShowSkillInfo(crrFocusSlot);
        UpdateGCoinResource();

        gameObject.SetActive(true);
    }

    [SkipRename]
    public override void ClickBackBtn()
    {
        SoundManager.instance.PlaySFX(SoundEnum.uiClickBtn);
        if(GuildManager.instance.myGuildInfo != null)
            GameUIManager.pageSwapper.SwitchPanel(UIPanelEnum.GuildHome);
        else
            GameUIManager.pageSwapper.SwitchPanel(UIPanelEnum.Main);
    }

    public override void ClosePage()
    {
        gameObject.SetActive(false);
    }
    #endregion

    #region Func
    public void ShowSkillInfo(GuildPassiveSlot slot)
    {
        crrFocusSlot = slot;
        csv = CSVParser.GetGuildPassiveCSV(slot.gPassiveId);


        if (GuildManager.instance.GetPassiveLv(slot.gPassiveId) != 0)
            HasSkill = true;
        else
            HasSkill = false;

        crrFocusSkillLevel = GuildManager.instance.GetPassiveLv(csv.gPassiveId);
        passiveImage.sprite = ResourceManager.GetInstance().GetSkillImage(csv.gPassiveImageName);

        levelCsv = CSVParser.GetGuildPassiveLvCSV(slot.gPassiveId, crrFocusSkillLevel);

        txtPassiveName.text = LocalizationSystem.GetLocalizedText( csv.gPassiveName);
        txtLevel.text = $"LV.{crrFocusSkillLevel}";

        //passiveFrameImage.sprite = slot.frameSprite;

        UpdateUpgradeButton();

        UpdateSkillDescription(slot);
        UpdateGCoinResource();
    }

    void UpdateUpgradeButton()
    {
        if (!HasSkill)
        {
            btnUpgrade.gameObject.SetActive(false);
            btnOpen.gameObject.SetActive(true);

            price = csv.openItemCnt;
            txtOpenPrice.text = price.ToString();
            payOpenItemImage.sprite = ResourceManager.GetInstance().GetConsumables(csv.openItemRefId);
            return;
        }
        else
        {
            payUpgradeItemImage.sprite = ResourceManager.GetInstance().GetConsumables(csv.upgradePayItemId);
            
            btnOpen.gameObject.SetActive(false);
            btnUpgrade.gameObject.SetActive(true);

            if (CSVParser.IsOverGuildPassiveLastLv(crrFocusSlot.gPassiveId, crrFocusSkillLevel+1) || levelCsv == null)
            {
                btnUpgrade.image.sprite = blockedSprite;
                btnUpgrade.interactable = false;
                txtLevel.text = "Max";
                txtUpgradePrice.text = MessageContainer.MaxLevel;
            }
            else
            {
                btnUpgrade.image.sprite = originSprite;
                btnUpgrade.interactable = true;

                price = csv.upgradeStartCost;
                if (crrFocusSkillLevel > 1) price += csv.upgradeIncCostValue * (crrFocusSkillLevel - 1);

                txtUpgradePrice.text = price.ToString();
            }

        }
    }

    void UpdateSkillDescription(GuildPassiveSlot slot)
    {
        if (levelCsv != null)
        {
            GuildPassiveCSV passiveCSV = CSVParser.GetGuildPassiveCSV(slot.gPassiveId);

            string format = LocalizationSystem.GetLocalizedText(passiveCSV.description);
            double incVal = levelCsv.args.Last();
            double totalVal = (double)CSVParser.GetTotalPassiveValue(slot.gPassiveId, slot.gPassiveLv);

            txtDescription.text = string.Format(format, ResourceConverter.GetUnitExpression(incVal), ResourceConverter.GetUnitExpression(totalVal));
        }
        else if(!HasSkill)
        {
            txtDescription.text = LocalizationSystem.GetLocalizedText("skillnotlearnmsg");
        }
        else
        {
            txtDescription.text = MessageContainer.MaxLevelMsg;
        }
    }
    #endregion

    void UpdateGCoinResource()
    {
        long item = UserData.ItemDB.GetCount(ItemEnum.GuildCoin);
        txtGCoinCnt.text = $"{ResourceConverter.GetUnitExpression(item)}";
        imgGCoinIcon.sprite = ResourceManager.GetInstance().GetRewardSprite(RewardType.ITEM, (int)ItemEnum.GuildCoin);
    }

    #region Button
    [SkipRename]
    public void OpenPassiveSkill()
    {
        if (crrFocusSkillLevel != 0) return;

        if (UserData.instance.GetCount(csv.needItemType, csv.openItemRefId) < csv.openItemCnt)
        {
            PostAlarmManager.instance.CreateAlarmPost(MessageContainer.NeedMoreMoneyMsg);
            return;
        }
        
        if (!CheckUpdatedGuildPassive(crrFocusSlot.gPassiveId))
        {
            loadingImg.SetActive(true);

            GuildManager.instance.myGuildInfo.guildPassive[crrFocusSlot.gPassiveId].gPassiveLv += 1;

            type = UseType.Open;
            IdleBackend.instance.UpdateGuildPassive(GuildManager.instance.myGuildInfo.guildPassive, onSuccess, onFailed);
        }
        else
        {
            PostAlarmManager.instance.CreateAlarmPost(GuildMessage.GuildInfoChangedMsg);
        }

        OpenPage();
    }

    [SkipRename]
    public void UpgradePassiveSkill()
    {
        if (crrFocusSkillLevel < 1) return;

        if (UserData.instance.GetCount(csv.needItemType, csv.upgradePayItemId) < price)
        {
            PostAlarmManager.instance.CreateAlarmPost(MessageContainer.NeedMoreMoneyMsg);
            return;
        }

        if (!CheckUpdatedGuildPassive(crrFocusSlot.gPassiveId))
        {
            int guildLv = GuildManager.instance.guildLv;
            if (crrFocusSkillLevel >= CSVParser.guildLevels[guildLv].maxPassiveLv)
            {
                PostAlarmManager.instance.CreateAlarmPost(MessageContainer.MaxLevelMsg);
                return;
            }

            if (CSVParser.IsOverGuildPassiveLastLv(crrFocusSlot.gPassiveId, crrFocusSkillLevel+1))
            {
                PostAlarmManager.instance.CreateAlarmPost(MessageContainer.MaxLevelMsg);
                return;
            }

            loadingImg.SetActive(true);

            GuildManager.instance.myGuildInfo.guildPassive[crrFocusSlot.gPassiveId].gPassiveLv += 1;
            
            
            type = UseType.Upgrade;
            IdleBackend.instance.UpdateGuildPassive(GuildManager.instance.myGuildInfo.guildPassive, onSuccess, onFailed);
        }
        else
        {
            AlarmPopupManager.instance.OpenAlarm(GuildMessage.GuildInfoChangedMsg, OpenPage);
        }            
    }

    /// <summary>
    /// 길드 패시브(id)에 변경된 정보가 있는지 확인해주는 함수.
    /// </summary>
    /// <param name="update"></param>
    public bool CheckUpdatedGuildPassive(int gPassiveId)
    {
        int origin = GuildManager.instance.myGuildInfo.guildPassive[gPassiveId].gPassiveLv;

        GuildManager.instance.UpdateMyGuildInfo();

        int updated = GuildManager.instance.myGuildInfo.guildPassive[gPassiveId].gPassiveLv;

        //값의 변경이 있을 경우 true, 없을 경우 false 리턴
        if (updated != origin)
            return true;
        else
            return false;
    }
    #endregion

    #region ETC
    void onSuccessAction(string msg)
    {
        PostAlarmManager.instance.CreateAlarmPost($"{msg}");
        
        switch (type)
        {
            case UseType.Open:
                UseItem(UseType.Open);
                break;
            case UseType.Upgrade:
                UseItem(UseType.Upgrade);
                break;
        }

        type = UseType.None;
        GuildManager.instance.UpdateMyGuildInfo();

        BackendLogger.SendLog(BackendLogType.GuildPassiveUp, $"Passive Up Success, PassiveId : {crrFocusSlot.gPassiveId}, PassiveCost :{price}");
    }

    void onFailedAction(string msg)
    {
        PostAlarmManager.instance.CreateAlarmPost($"{msg}");
        GuildManager.instance.myGuildInfo.guildPassive[crrFocusSlot.gPassiveId].gPassiveLv -= 1;

        BackendLogger.SendLog(BackendLogType.GuildPassiveUp, $"Passive Up Failed, PassiveId : {crrFocusSlot.gPassiveId}, PassiveCost :{price}");

        loadingImg.SetActive(false);
        OpenPage();
    }

    void UseItem(UseType type)
    {
        src = new Source();
        
        switch (type)
        {
            case UseType.Open:
                src.itemType = csv.needItemType;
                src.itemId = csv.openItemRefId;
                src.ItemCnt = csv.openItemCnt;
                break;
            case UseType.Upgrade:
                src.itemType = csv.needItemType;
                src.itemId = csv.upgradePayItemId;
                src.ItemCnt = csv.upgradeStartCost + csv.upgradeIncCostValue * (crrFocusSkillLevel - 1);
                break;
        }

        IdleBackend.instance.UpdateGuildExpWithRank((int)src.ItemCnt, UseSuccess, UseFailed);
    }

    void UseSuccess()
    {
        UserData.instance.Use(src);
        UserData.ItemDB.SaveImmediately((int)ItemEnum.GuildCoin);

        loadingImg.SetActive(false);
        mgr.Initialize();
        OpenPage();
    }

    void UseFailed(string msg)
    {
        UserData.instance.GainReward(src);

        onFailedAction(msg);
    }
    #endregion
}
