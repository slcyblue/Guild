using Beebyte.Obfuscator;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GuildListPage : UIPage
{

    [SerializeField] public GameObject createField = null;
    [SerializeField] GuildRankListPage guildRankList = null;
    [SerializeField] GuildRandListPage guildRandList = null;
    [SerializeField] List<Image> categories = null;
    [SerializeField] public GameObject loadingImg = null;
    [SerializeField] Sprite active = null;
    [SerializeField] Sprite inActive = null;
    [SerializeField] Text txtSearchName = null;

    private float refreshCool = 0f;

    GuildListCategory crrIdx = GuildListCategory.Rank;

    public enum GuildListCategory
    {
        Rank,
        Rand
    }
    #region Init
    private void Update()
    {
        if (refreshCool > 0f) refreshCool -= Time.deltaTime;
    }

    public override void OpenPage()
    {
        gameObject.SetActive(true);

        if (GuildManager.instance.myGuildInfo == null)
            createField.SetActive(true);
        else
            createField.SetActive(false);

        GuideManager.instance.OpenGuideDialog(GuideIdEnum.Guild);
    }

    public override void ClosePage()
    {
        Initialize();
        gameObject.SetActive(false);
    }

    [SkipRename]
    public void ShowHelp()
    {
        GuideManager.instance.ReOpenGuideDialog(GuideIdEnum.Guild);
    }

    public override void Initialize()
    {
        txtSearchName.text = "";
    }
    #endregion

    #region Category
    [SkipRename]
    public void ChangeCategory(int type)
    {
        if (refreshCool > 0f)
        {
            PostAlarmManager.instance.CreateAlarmPost(GuildMessage.RefreshCoolTimeMsg);
            return;
        }

        ChangeCategory((GuildListCategory)type);
    }

    [SkipRename]
    public void ChangeCategory(GuildListCategory type)
    {
        SoundManager.instance.PlaySFX(SoundEnum.uiClickBtn);
        ChangePage(type);
        ChangeFocus(type);
    }

    public void ChangePage(GuildListCategory category)
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            gameObject.transform.SetAsLastSibling();
        }

        switch (category)
        {
            case GuildListCategory.Rank:
                guildRankList.gameObject.SetActive(true);
                try
                {
                    guildRankList.OpenPage(this);
                }
                catch (Exception e)
                {
                    Debug.Log(e.Message);
                }
                guildRandList.gameObject.SetActive(false);
                
                refreshCool = 5f;
                break;
            case GuildListCategory.Rand:
                guildRandList.gameObject.SetActive(true);
                try
                {
                    guildRandList.OpenPage(this);
                }
                catch (Exception e)
                {
                    Debug.Log(e.Message);
                }
                guildRankList.gameObject.SetActive(false);

                refreshCool = 5f;
                break;
        }
    }

    [SkipRename]
    public void ChangeFocus(GuildListCategory idx)
    {        
        crrIdx = idx;

        for (int i = 0; i < Enum.GetValues(typeof(GuildListCategory)).Length; i++)
        {
            if (i == (int)crrIdx) categories[i].sprite = active;

            else categories[i].sprite = inActive;
        }
    }
    #endregion


    #region btnEvent
    [SkipRename]
    public override void ClickBackBtn()
    {
        SoundManager.instance.PlaySFX(SoundEnum.uiClickBtn);
        if (GuildManager.instance.myGuildInfo != null)
            GameUIManager.pageSwapper.SwitchPanel(UIPanelEnum.GuildHome);
        else
            GameUIManager.pageSwapper.SwitchPanel(UIPanelEnum.Main);
    }
    

    [SkipRename]
    public void SearchGuild()
    {
        if (refreshCool > 0f)
        {
            PostAlarmManager.instance.CreateAlarmPost(GuildMessage.RefreshCoolTimeMsg);
            return;
        }

        if (txtSearchName == null || txtSearchName.text.Length < 2 || txtSearchName.text.Length > 8)
        {
            PostAlarmManager.instance.CreateAlarmPost(GuildMessage.InvalidNameMsg);
            return;
        }

        loadingImg.SetActive(true);
        IdleBackend.instance.FindGuildWithName(txtSearchName.text, result => {
            switch (crrIdx)
            {
                case GuildListCategory.Rank:
                    guildRankList.guildInfos.Clear();
                    guildRankList.guildInfos.Add(result);
                    guildRankList.height = 0f;
                    guildRankList.RelocateItems();
                    loadingImg.SetActive(false);
                    break;
                case GuildListCategory.Rand:
                    guildRandList.guildRandInfos.Clear();
                    guildRandList.guildRandInfos.Add(result);
                    guildRandList.height = 0f;
                    guildRandList.RelocateItems();
                    loadingImg.SetActive(false);
                    break;
            }
            refreshCool = 5f;
            loadingImg.SetActive(false);
        }, msg => {
            PostAlarmManager.instance.CreateAlarmPost(msg);
            refreshCool = 5f;
            loadingImg.SetActive(false);
        });
    }

    [SkipRename]
    public void OpenCreateGuildPopup()
    {
        if (!UserData.accountDB.IsJoinableNewGuild())
        {
            PostAlarmManager.instance.CreateAlarmPost(GuildMessage.GuildCooltimeMsg);
            return;
        }

        PopupManager.instance.OpenPopup(PopupType.GuildPopup, result=> {
            result.GetComponent<GuildPopup>().ChangePage(GuildPopup.PopupPage.CREATE);
        });
    }

    /// <summary>
    /// 길드 목록 새로고침 버튼을 눌렀을 경우 실행되는 함수
    /// </summary>
    [SkipRename]
    public void RefreshItems()
    {
        if (refreshCool > 0f)
        {
            PostAlarmManager.instance.CreateAlarmPost(GuildMessage.RefreshCoolTimeMsg);
            return;
        }

        loadingImg.SetActive(true);
        switch (crrIdx)
        {
            case GuildListCategory.Rank:
                guildRankList.OpenPage(this);                               
                PostAlarmManager.instance.CreateAlarmPost(MessageContainer.SuccessRefresh);
                break;
            case GuildListCategory.Rand:
                guildRandList.OpenPage(this);               
                PostAlarmManager.instance.CreateAlarmPost(MessageContainer.SuccessRefresh);
                break;
        }

        refreshCool = 5f;
        loadingImg.SetActive(false);
    }
    #endregion
}
