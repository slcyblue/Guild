using BackEnd;
using Beebyte.Obfuscator;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GuildHomePage : UIPage
{
    #region Variables
    [SerializeField] ResourceUpdater guildCoinUpdater = null;

    [SerializeField] Text txtGuildName = null;
    [SerializeField] Text txtGuildNotice = null;
    [SerializeField] Text txtGuildComment = null;
    //[SerializeField] Text txtGuildMasterName = null;
    [SerializeField] Text txtGuildMemCnt = null;
    [SerializeField] Slider txtGuildExp = null;
    [SerializeField] Text txtGuildExpText = null;
    [SerializeField] Text txtGuildLv = null;
    [SerializeField] Image guildIconImg = null;

    [SerializeField] Text txtIncExpCost = null;
    [SerializeField] Image imgIncExpItem = null;

    [SerializeField] Button btnGuildManage = null;
    //[SerializeField] Button btnGuildPvp = null;
    //[SerializeField] Button btnGuildRequest = null;

    //[SerializeField] Sprite LockImage = null;
    [SerializeField] public GameObject loadingImg = null;

    private float refreshCool = 0f;
    private float updatedCool = 0f;
    public Action useResourceEvent;
    #endregion



    #region Init
    private void Update()
    {
        if (refreshCool > 0f) refreshCool -= Time.deltaTime;
        if (updatedCool > 0f) updatedCool -= Time.deltaTime;
    }

    public override void OpenPage()
    {
        Initialize();
        
        if (UserData.accountDB.IsGuildAttendanceReceievable())
            OpenGuildAtdPopup();

        gameObject.SetActive(true);
    }

    [SkipRename]
    public override void ClosePage()
    {
        gameObject.SetActive(false);
    }

    public override void Initialize()
    {
        if (GuildManager.instance != null && GuildManager.instance.myGuildInfo != null)
        {            
            if ((int)GuildManager.instance.myGrade < 2) btnGuildManage.gameObject.SetActive(false);
            else btnGuildManage.gameObject.SetActive(true);

            InitUI();
            UpdateResource();
        }
    }

    
    #endregion

    #region UI
    void InitUI()
    {
        int guildLv = CSVParser.GetGuildLv(GuildManager.instance.myGuildInfo.guildExp);
        long reqExp = CSVParser.GetReqExpCnt(GuildManager.instance.myGuildInfo.guildExp);

        if (guildLv > CSVParser.guildLevels.Count) guildLv = CSVParser.guildLevels.Count;

        txtGuildLv.text = string.Format(GuildMessage.GuildLevelWord, guildLv);
        txtGuildExp.value = GuildManager.instance.myGuildInfo.guildExp;
        txtGuildExp.maxValue = reqExp;

        if (GuildManager.instance.myGuildInfo.guildExp >= CSVParser.guildLevels[CSVParser.guildLevels.Count].reqGuildExp)
            txtGuildExpText.text = "MAX";
        else
            txtGuildExpText.text = $"{ GuildManager.instance.myGuildInfo.guildExp}/{reqExp}";

        txtGuildName.text = GuildManager.instance.myGuildInfo.guildName;
        txtGuildNotice.text = GuildManager.instance.myGuildInfo.guildNotice;
        txtGuildComment.text = GuildManager.instance.myGuildInfo.guildComment;
        //txtGuildMasterName.text = _myGuildInfo.masterNickname;

        txtIncExpCost.text = $"{ResourceConverter.GetUnitExpression(CSVParser.GetGlobalIntVariable(GlobalVariables.guildIncExpCost))}°³";
        imgIncExpItem.sprite = ResourceManager.GetInstance().GetConsumables((int)ItemEnum.GuildCoin);

        txtGuildMemCnt.text = $"{GuildManager.instance.myGuildInfo.memberCount}/{CSVParser.GetMaxMemberCnt(guildLv) + UserData.buffMgr.GetBuffValue(AccountBuffEnum.GuildLvUpgrade, 1)}";

        if (string.IsNullOrEmpty(GuildManager.instance.myGuildInfo.guildIconName)) guildIconImg.sprite = ResourceManager.GetInstance().GetGuildIconSprite("1");
        else guildIconImg.sprite = ResourceManager.GetInstance().GetGuildIconSprite(GuildManager.instance.myGuildInfo.guildIconName);
    }
    #endregion

    #region Func
    [SkipRename]
    public void LevelUp()
    {
        if (updatedCool > 0f)
        {
            PostAlarmManager.instance.CreateAlarmPost(GuildMessage.RefreshCoolTimeMsg);
            return;
        }

        if (GuildManager.instance != null && GuildManager.instance.myGuildInfo != null)
        {
            if (GuildManager.instance.myGuildInfo.guildExp >= CSVParser.guildLevels[CSVParser.guildLevels.Count].reqGuildExp)
            {
                PostAlarmManager.instance.CreateAlarmPost(MessageContainer.MaxLevelMsg);
                return;
            }
            
            if (UserData.ItemDB.GetCount(ItemEnum.GuildCoin) < CSVParser.GetGlobalIntVariable(GlobalVariables.guildIncExpCost))
            {
                PostAlarmManager.instance.CreateAlarmPost(MessageContainer.NeedMoreMoneyMsg);
                return;
            }

            useResourceEvent = UseResource;
            loadingImg.SetActive(true);
            GuildManager.instance.UpdateGuildExp(this);
        }
        else
        {
            PostAlarmManager.instance.CreateAlarmPost(GuildMessage.GuildNotExist);
            ClosePage();
        }
    }

    [SkipRename]
    public void OpenGuildAtdPopup()
    {
        PopupManager.instance.OpenPopup(PopupType.Attendance,
            callback => {
                callback.GetComponent<AttendancePopup>().SetRewards(AttendancePopup.AttendenceType.Guild);
                callback.GetComponent<AttendancePopup>().AddListener(UpdateResource);
            });
    }

    [SkipRename]
    public void OpenGuildManagePopup()
    {
        SoundManager.instance.PlaySFX(SoundEnum.uiClickBtn);
        if ((int)GuildManager.instance.myGrade < 2)
        {
            PostAlarmManager.instance.CreateAlarmPost(GuildMessage.NotHaveAuthorityMsg);
            return;
        }

        PopupManager.instance.OpenPopup(PopupType.GuildMngPopup, callback => callback.GetComponent<GuildManagePopup>().ChangePage(GuildManagePopup.PopupPage.Modify)) ;
    }

    [SkipRename]
    public void OpenGuildListPage()
    {
        SoundManager.instance.PlaySFX(SoundEnum.uiClickBtn);
        GameUIManager.pageSwapper.SwitchPanel(UIPanelEnum.GuildList, result => {
            result.GetComponent<GuildListPage>().ChangeCategory(GuildListPage.GuildListCategory.Rank);
            });
    }

    [SkipRename]
    public void OpenGuildPassivePage()
    {
        SoundManager.instance.PlaySFX(SoundEnum.uiClickBtn);
        GameUIManager.pageSwapper.SwitchPanel(UIPanelEnum.GuildPassive, callback => callback.GetComponent<GuildPassivePage>().mgr = this);
    }
    [SkipRename]
    public void OpenGuildShopPage()
    {
        SoundManager.instance.PlaySFX(SoundEnum.uiClickBtn);
        GameUIManager.pageSwapper.SwitchPanel(UIPanelEnum.GuildShop);
    }
    [SkipRename]
    public void OpenGuildDungeonPopup()
    {
        SoundManager.instance.PlaySFX(SoundEnum.uiClickBtn);
        GuildManager.instance.UpdateMyGuildInfo();
        PopupManager.instance.OpenPopup(PopupType.GuildDungeonPopup, callback => {
            if (!(callback is GuildDungeonPopup popup))
                return;
            popup.SetGuildHomePage(this);
        });
    }

    [SkipRename]
    public void OpenMemberListPopup()
    {
        SoundManager.instance.PlaySFX(SoundEnum.uiClickBtn);
        PopupManager.instance.OpenPopup(PopupType.GuildMemListPopup, callback => {
            callback.GetComponent<GuildMemListPopup>().ChangePage(GuildMemListPopup.PopupPage.MemberList);
        });
    }

    [SkipRename]
    public void OpenGuildPVPPopup()
    {
        SoundManager.instance.PlaySFX(SoundEnum.uiClickBtn);
        PostAlarmManager.instance.CreateAlarmPost(MessageContainer.UpdateLaterMsg);
    }

    [SkipRename]
    public void OpenGuildRequestPopup()
    {
        SoundManager.instance.PlaySFX(SoundEnum.uiClickBtn);
        PostAlarmManager.instance.CreateAlarmPost(MessageContainer.UpdateLaterMsg);
    }

    [SkipRename]
    public override void ClickBackBtn()
    {
        SoundManager.instance.PlaySFX(SoundEnum.uiClickBtn);
        GameUIManager.pageSwapper.SwitchPanel(UIPanelEnum.Main);
    }
    #endregion

    #region etc
    public void UpdateResource()
    {
        guildCoinUpdater.UpdateResource((int)RewardType.ITEM, (int)ItemEnum.GuildCoin,  UserData.ItemDB.GetCount(ItemEnum.GuildCoin));
    }

    void UseResource()
    {
        UserData.ItemDB.UseItem((int)ItemEnum.GuildCoin, CSVParser.GetGlobalIntVariable(GlobalVariables.guildIncExpCost));
        UserData.ItemDB.SaveImmediately((int)ItemEnum.GuildCoin);

        BackendLogger.SendLog(BackendLogType.GuildLevelUp, $"Lv Up Success, leftoverItemCnt : {UserData.ItemDB.GetCount(ItemEnum.GuildCoin)}");
        GuildManager.instance.UpdateMyGuildInfo();

        Initialize();
        loadingImg.SetActive(false);
        updatedCool = 5f;
    }

    public void CreateAlarmMsg(string msg)
    {
        AlarmPopupManager.instance.OpenAlarm($"{msg}", ()=> {
            BackendLogger.SendLog(BackendLogType.GuildLevelUp, $"Lv Up Failed, leftoverItemCnt : {UserData.ItemDB.GetCount(ItemEnum.GuildCoin)}");
            Initialize(); 
            loadingImg.SetActive(false); 
        });

        updatedCool = 5f;
    }

    [SkipRename]
    public void RefreshGuildInfo()
    {
        if (refreshCool > 0f)
        {
            PostAlarmManager.instance.CreateAlarmPost(GuildMessage.RefreshCoolTimeMsg);
            return;
        }

        GuildManager.instance.UpdateMyGuildInfo();
        PostAlarmManager.instance.CreateAlarmPost(GuildMessage.SuccessRefreshGuildInfo);
        Initialize();

        refreshCool = 300f;
    }
    #endregion
}
