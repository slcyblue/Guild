using Beebyte.Obfuscator;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GuildRandListSlot : MonoBehaviour
{
    [SerializeField] RectTransform rect = null;
    [SerializeField] Text txtGuildName = null;
    [SerializeField] Text txtGuildLv = null;
    [SerializeField] Text txtGuildAttribute = null;
    [SerializeField] Text txtGuildComment = null;
    [SerializeField] Text txtGuildMemCnt = null;
    [SerializeField] Image guildIconImg = null;
    [SerializeField] Text txtApplyBtn = null;

    GuildRandListPage guildMgr;

    public GameObject btnApply = null;
    public GuildInfo _guildInfo = null;
    int _guildLv;
    bool updated = true;

    public void UpdateItem(GuildInfo guildRandInfo, GuildRandListPage mgr = null)
    {
        gameObject.SetActive(true);
        guildMgr = mgr; //�� ������������ ����� GuildListPage�� ����� ����ϱ� ���� ����
        _guildInfo = guildRandInfo;

        txtGuildName.text = _guildInfo.guildName;
        txtGuildComment.text = _guildInfo.guildComment;

        _guildLv = CSVParser.GetGuildLv(_guildInfo.guildExp);
        txtGuildLv.text = _guildLv.ToString();
        txtGuildAttribute.text = string.Format(MessageContainer.PointFormat, ResourceConverter.GetUnitExpression(_guildInfo.guildExp));

        int maxCnt = CSVParser.GetMaxMemberCnt(_guildInfo.guildExp) + (int)CSVParser.GetTotalPassiveValue(_guildInfo.guildPassive[9].gPassiveId, _guildInfo.guildPassive[9].gPassiveLv);
        txtGuildMemCnt.text = $"{_guildInfo.memberCount}/{maxCnt}";
        
        if (string.IsNullOrEmpty(_guildInfo.guildIconName))
            guildIconImg.sprite = ResourceManager.GetInstance().GetGuildIconSprite("1");
        else
            guildIconImg.sprite = ResourceManager.GetInstance().GetGuildIconSprite(_guildInfo.guildIconName);

        if (!GuildManager.instance.checkGuildMemCnt(_guildInfo) || GuildManager.instance.myGuildInfo != null) btnApply.SetActive(false);
        else if (_guildInfo._immediateRegistration) txtApplyBtn.text = GuildMessage.JoinImmediately;
        else txtApplyBtn.text = GuildMessage.SendRegistWord;
    }

    public void SetPosition(float yPos)
    {
        rect.anchoredPosition = new Vector2(0, -1 * yPos);
    }
            

    [SkipRename]
    public void GuildDetail()
    {
        PopupManager.instance.OpenPopup(PopupType.GuildPopup, (callback)=> {
            callback.GetComponent<GuildPopup>().ChangePage(GuildPopup.PopupPage.INFO, _guildInfo);
        });
    }

    [SkipRename]
    public void ApplyGuild()
    {
        if (!UserData.accountDB.IsJoinableNewGuild())
        {
            PostAlarmManager.instance.CreateAlarmPost(GuildMessage.GuildCooltimeMsg);

            //AlarmPopupManager.instance.OpenAlarm("Ż������ 24�ð��� ������ �ʾҽ��ϴ�.", () => StartCoroutine(ShowRemainTime));
            guildMgr.mgr.loadingImg.SetActive(false);
            return;
        }

        //��� ���� ���� Ȯ��
        if (!CheckApplyCondition(_guildInfo))
        {
            PostAlarmManager.instance.CreateAlarmPost(GuildMessage.NotMatchGuildCondition);
            return;
        }


        StartCoroutine(CoApplyGuild());
    }

    IEnumerator CoApplyGuild()
    {
        guildMgr.mgr.loadingImg.SetActive(true);
        updated = true;

        //��û�ϱ� �� ��带 �ֽ� ������ ������Ʈ.
        StartCoroutine(IdleBackend.instance.GetGuildInfo(_guildInfo.Indate, result =>
        {
            _guildInfo = result;
            updated = false;
        }));

        while (updated) yield return new WaitForSeconds(.1f);

        checkGuildCondAndApply();
        guildMgr.mgr.loadingImg.SetActive(false);
    }

    void checkGuildCondAndApply()
    {
        //����� �� Ȯ��
        if (!GuildManager.instance.checkGuildMemCnt(_guildInfo))
        {
            PostAlarmManager.instance.CreateAlarmPost(GuildMessage.MaxGuildMemberMsg);
            UpdateItem(_guildInfo);
            return;
        }

        IdleBackend.instance.ApplyGuild(_guildInfo.Indate, onSuccess, onFailed);
    }

    /// <summary>
    /// ��� ��û�� ���������� �����ϴ��� �˻����ִ� �Լ�
    /// </summary>
    /// <param name="_guildinfo"></param>
    /// <returns></returns>
    public bool CheckApplyCondition(GuildInfo _guildinfo)
    {
        if (UserData.accountDB.LastClearStage < _guildinfo.limitStage || UserData.accountDB.userLevel < _guildinfo.limitLv)
            return false;
        else
            return true;
    }

    void onSuccess()
    {
        if (_guildInfo._immediateRegistration)
        {
            PostAlarmManager.instance.CreateAlarmPost(GuildMessage.SuccessJoinGuildMsg);

            GuildManager.instance.UpdateMyGuildInfo();

            GameUIManager.pageSwapper.SwitchPanel(UIPanelEnum.GuildHome, callback => {
                if (UserData.accountDB.IsGuildAttendanceReceievable())
                    callback.GetComponent<GuildHomePage>().OpenGuildAtdPopup();
            });
        }
        else
        {
            PostAlarmManager.instance.CreateAlarmPost(GuildMessage.SuccessGuildProposalMsg);
        }
    }

    void onFailed(string msg)
    {
        PostAlarmManager.instance.CreateAlarmPost(msg);
    }
}
