using BackEnd;
using BackEnd.Tcp;
using Beebyte.Obfuscator;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class GuildManager : MonoSingleton<GuildManager> 
{ 
    #region Variable
    public GuildInfo myGuildInfo = null;

    public int guildLv { get; private set; } = 0;

    Dictionary<int, GuildPassive> tmpGuildPassive = null;

    OnModifyEvent modifyEvent;

    Action<string> onFailedAction = null;
    public bool isFinished = false;
    #endregion
    public enum GuildMemberGrade
    {
        none,
        member,
        viceMaster,
        master
    }

    public struct userInfoValue
    {
        public string battlePower, stageVal, userIndate, userName, gamerIndate, position;
        public Dictionary<int, int> userEquip;
        public Sprite userIcon;
    }

    public int MaxLength { get; private set; }
    public int MinLength { get; private set; }

    public GuildMemberGrade myGrade = GuildMemberGrade.none;


    public void AddListener(OnModifyEvent action)
    {
        modifyEvent = action;
    }

    public override void Initialize()
    {
        base.Initialize();

        MaxLength = CSVParser.GetGlobalIntVariable(GlobalVariables.guildNameMaxMonoLength);
        MinLength = CSVParser.GetGlobalIntVariable(GlobalVariables.guildNameMinMonoLength);
    }
    
    /// <summary>
    /// 유저가 가입된 길드가 있는지 확인하고 있을 경우 길드정보를, 없을 경우 null을 리턴해주는 함수.
    /// </summary>
    /// <returns></returns>
    public GuildInfo UpdateMyGuildInfo()
    {
        myGuildInfo = IdleBackend.instance.GetMyGuildInfo(ErrorMsg);
     
        if (myGuildInfo != null)
        {
            AccessGrade();
            UpdateGuildPassiveEffect(myGuildInfo.guildPassive);

            if(guildLv != 0 && guildLv != CSVParser.GetGuildLv(myGuildInfo.guildExp))
            {
                guildLv = CSVParser.GetGuildLv(myGuildInfo.guildExp);
                //레벨업시 상점 품목 갱신
                UserData.ShopDB.GenRandVal(guildLv);                
            }
            else if(guildLv == 0)
                    guildLv = CSVParser.GetGuildLv(myGuildInfo.guildExp);
        }
        else
        {
            //길드정보가 없는데 tmpGuildPassive가 저장되어 있는 경우 tmpGuildPassive의 내용을 제거
            if(tmpGuildPassive != null)
            {
                //buffMng에 등록된 정보 제거
                for(int i =0; i<tmpGuildPassive.Count; i++)
                {
                    if (tmpGuildPassive[i + 1].gPassiveLv == 0) continue;

                    for(int j = tmpGuildPassive[i + 1].gPassiveLv; j>0; j--)
                    {
                        GuildPassiveLvCSV guildPassiveData = CSVParser.GetGuildPassiveLvCSV(i+1, j);
                        UserData.buffMgr.RemoveBuff(guildPassiveData.accountBuffType, guildPassiveData.args);
                    }
                }

                tmpGuildPassive.Clear();
                UserData.GuildDB.ResetChallengeLv();
            }

            myGrade = GuildMemberGrade.none;

            //if(Backend.Chat.IsChatConnect(ChannelType.Guild))
            //    ChatManager.instance.LeaveGuildChannel();
        }

        UserData.StatDB.CalculateTotalStat();

        modifyEvent?.Invoke();
        return myGuildInfo;
    }

    void UpdateGuildPassiveEffect(Dictionary<int, GuildPassive> guildPassive)
    {
        CheckNewPassive(guildPassive);
        
        if (tmpGuildPassive == null || tmpGuildPassive.Count == 0)
        {
            tmpGuildPassive = new Dictionary<int, GuildPassive>();

            for (int i = 0; i<guildPassive.Count; i++)
            {
                //tmpGuilldPassive에 등록
                tmpGuildPassive.Add(i + 1, guildPassive[i + 1]);

                //패시브 레벨이 0일 경우 무시.
                if (guildPassive[i + 1].gPassiveLv == 0) continue;

                //패시브 정보가 없을 경우 무시.
                if (!guildPassive.TryGetValue(i + 1, out GuildPassive value)) continue;

                //패시브 Id에 해당하는 패시브 데이터를 가져옴.
                GuildPassiveLvCSV guildPassiveData = CSVParser.GetGuildPassiveLvCSV(value.gPassiveId, value.gPassiveLv);

                if (guildPassiveData == null) continue;

                float[] temp = new float[guildPassiveData.args.Length];

                for (int j = 0; j < temp.Length; j++)
                {
                    temp[j] = guildPassiveData.args[j];
                }

                //패시브 레벨에 따라 증가하는 버프의 총량을 계산
                float totalVal = (float)CSVParser.GetTotalPassiveValue(value.gPassiveId, value.gPassiveLv);

                //패시브 레벨까지 계산된 버프의 총량을 패시브 데이터에 등록.
                temp[temp.Length - 1] = totalVal;
                
                //패시브 데이터를 buffMng에 등록
                UserData.buffMgr.AddBuff(guildPassiveData.accountBuffType, temp);
            }
        }
        else
        {
            for(int i = 0; i<guildPassive.Count; i++)
            {
                //tmpGuildPassive에 저장된 값과 업데이트한 길드 패시브 값을 비교 후 다를 경우 buffMng에 등록.
                if(guildPassive[i+1].gPassiveLv != 0 && tmpGuildPassive[i+1].gPassiveLv != guildPassive[i + 1].gPassiveLv)
                {
                    GuildPassiveLvCSV guildPassiveData = CSVParser.GetGuildPassiveLvCSV(i+1, guildPassive[i + 1].gPassiveLv);
                    
                    double originVal = CSVParser.GetTotalPassiveValue(i + 1, tmpGuildPassive[i + 1].gPassiveLv);
                    double newVal = CSVParser.GetTotalPassiveValue(i + 1, guildPassive[i + 1].gPassiveLv);

                    float[] temp = new float[guildPassiveData.args.Length];

                    for (int j = 0; j < temp.Length; j++)
                    {
                        temp[j] = guildPassiveData.args[j];
                    }

                    temp[temp.Length - 1] = (float)(newVal - originVal);
                    UserData.buffMgr.AddBuff(guildPassiveData.accountBuffType, temp);
                    
                    tmpGuildPassive[i + 1].gPassiveLv = guildPassive[i + 1].gPassiveLv;
                }
            }
        }
    }

    //CSV데이터에 새로운 스킬이 추가 됐을 경우 길드파라미터에 추가해주는 함수.
    void CheckNewPassive(Dictionary<int, GuildPassive> guildPassive)
    {
        if(guildPassive.Count < CSVParser.guildPassives.Count)
        {
            foreach(var item in CSVParser.guildPassives)
            {
                if (guildPassive.ContainsKey(item.Key)) continue;
                else
                {
                    GuildPassive temp = new GuildPassive();
                    temp.gPassiveId = item.Value.gPassiveId;
                    temp.gPassiveLv = 0;
                    guildPassive.Add(item.Key, temp);
                }
            }
            IdleBackend.instance.UpdateGuildPassive(guildPassive, ErrorMsg);
        }
    }

    /// <summary>
    /// 자신의 길드 내 등급을 확인하는 함수
    /// </summary>
    void AccessGrade()
    {
        string myNickname = UserData.accountDB.nickName;

        if (myNickname == myGuildInfo.masterNickname) myGrade = GuildMemberGrade.master;
        else if (isViceMaster()) myGrade = GuildMemberGrade.viceMaster;
        else myGrade = GuildMemberGrade.member;
    }

    /// <summary>
    /// 길드 장로인지 확인해주는 함수
    /// </summary>
    /// <returns></returns>
    bool isViceMaster()
    {
        if (myGuildInfo.viceMasterList.Count == 0) return false;
       
        for (int i = 0; i < myGuildInfo.viceMasterList.Count; i++)
        {
            if (UserData.accountDB.nickName == myGuildInfo.viceMasterList[i].nickname) return true;
        }
        return false;
    }


    
    /// <summary>
    /// 길드 멤버 수를 체크하는 함수
    /// </summary>
    /// <param name="_guildinfo"></param>
    /// <returns></returns>
    [SkipRename]
    public bool checkGuildMemCnt(GuildInfo _guildinfo)
    {
        int maxMemCnt = CSVParser.GetMaxMemberCnt(_guildinfo.guildExp) + (int)CSVParser.GetTotalPassiveValue(_guildinfo.guildPassive[9].gPassiveId, _guildinfo.guildPassive[9].gPassiveLv);

        if (_guildinfo.memberCount >= maxMemCnt) return false;
        else
            return true;
    }

    public void UpdateGuildExp(GuildHomePage mgr)
    {
        long crrExp = myGuildInfo.guildExp;
        Action onSuccess = mgr.useResourceEvent;
        onFailedAction = mgr.CreateAlarmMsg;

        GuildInfo guildInfo = UpdateMyGuildInfo();

        if(guildInfo == null)
        {
            GameUIManager.pageSwapper.SwitchPanel(UIPanelEnum.Main);
            return;
        }

        if (guildInfo.guildExp != crrExp)
        {
            onFailedAction.Invoke(GuildMessage.GuildInfoChangedMsg);
            mgr.Initialize();
            return;
        }
        else
        {
            double incVal = UserData.buffMgr.GetGuildValue(AccountBuffEnum.GuildExpUpgrade);

            int updatedExp = Convert.ToInt32(CSVParser.GetGlobalIntVariable(GlobalVariables.guildIncExpVal) * (1+incVal/100));
            IdleBackend.instance.UpdateGuildExpWithRank(updatedExp, onSuccess, onFailedAction);
        }
    }

    /// <summary>
    /// 길드패시브id 에 해당하는 lv 반환.
    /// <param name="id"></param>
    /// <returns></returns>
    public int GetPassiveLv(int id)
    {
        GuildPassive guildPassive;
        if (myGuildInfo.guildPassive.TryGetValue(id, out guildPassive))
        {
            if (guildPassive.gPassiveLv != 0) return guildPassive.gPassiveLv;
            else return 0;
        }
        else return 0;
    }

    void ErrorMsg(string msg)
    {
        PostAlarmManager.instance.CreateAlarmPost(msg);
    }
}
