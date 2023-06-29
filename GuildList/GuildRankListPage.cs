using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GuildRankListPage : MonoBehaviour
{
    [SerializeField] float ITEM_HEIGHT = 0F;
    [SerializeField] GameObject guildPrefab = null;
    [SerializeField] RectTransform listParent = null;
    [SerializeField] GameObject txtHaveNotGuild = null;

    List<GuildRankListSlot> slotList = new List<GuildRankListSlot>();
    public List<GuildRank> guildRankInfos = null;
    public List<GuildInfo> guildInfos = null;

    public GuildListPage mgr;
    const int showCnt = 10;
    public float height = 0f;

    public void OpenPage(GuildListPage _mgr)
    {
        if (guildPrefab.activeSelf) guildPrefab.SetActive(false);

        mgr = _mgr;

        GetGuildRankInfo();
    }

    /// <summary>
    /// 뒤끝에 저장된 길드 랭킹 정보를 가져온다(20개,경험치순).
    /// </summary>
    void GetGuildRankInfo()
    {
        //기존데이터가 있을 경우 삭제하고 새로운 길드목록을 출력.
        if (guildRankInfos != null && guildRankInfos.Count != 0)
        {
            guildRankInfos.Clear();
            height = 0f;
        }

        mgr.loadingImg.SetActive(true);
        IdleBackend.instance.GetGuildRankList(showCnt, OnCompleteAction, onFailed);
    }

    void OnCompleteAction(List<GuildInfo> infos,List<GuildRank> ranks)
    {
        guildRankInfos = ranks;
        guildInfos = infos;

        //추출된 길드 정보가 없을 경우 처리.
        if (guildRankInfos == null || guildRankInfos.Count == 0)
        {
            txtHaveNotGuild.SetActive(true);

            if (slotList.Count > 0)
            {
                foreach(var item in slotList)
                {
                    item.gameObject.SetActive(false);
                }

                slotList.Clear();
            }
        }
        //추출된 길드 정보를 guildPrefab으로 생성
        else
        {
            SortGuildInfo(guildInfos);
            txtHaveNotGuild.SetActive(false);
            RelocateItems();
        }

        mgr.loadingImg.SetActive(false);
    }
    void onFailed(string msg)
    {
        if (!string.IsNullOrEmpty(msg))
            PostAlarmManager.instance.CreateAlarmPost(msg);

        mgr.loadingImg.SetActive(false);
    }
    void SortGuildInfo(List<GuildInfo> infos)
    {
        for (int i = 0; i < infos.Count; i++)
        {
            if (infos[i] == null || infos[i].memberCount <= 0) 
            {
                guildInfos.Remove(infos[i]);
                i--;
                continue;
            }
        }

        guildInfos = guildInfos.OrderByDescending(x => x.guildExp).ToList();
    }

    /// <summary>
    /// 길드목록 수에 따라 리스트 아이템을 생성 및 데이터 파싱을 해주는 함수.
    /// </summary>
    public void RelocateItems()
    {
        int length = guildInfos.Count;
        GuildRankListSlot slot = null;

        if(length == 0)
        {
            txtHaveNotGuild.SetActive(true);
            return;
        }

        for (int i = 0; i < length; i++)
        {
            if (slotList.Count <= i) slot = GetNewObject();
            else slot = slotList[i];

            if (GuildManager.instance.myGuildInfo != null)
                slot.btnApply.SetActive(false);
            else
                slot.btnApply.SetActive(true);

            slot.UpdateItem(guildInfos[i], this);
            slot.SetPosition(height);
            height += ITEM_HEIGHT;
            
        }

        for (int i = length; i < slotList.Count; i++)
        {
            slotList[i].gameObject.SetActive(false);
        }

        if (mgr.createField.activeSelf)
            height += mgr.createField.GetComponent<RectTransform>().rect.height;
        
        listParent.sizeDelta = new Vector2(0, height);
    }

    /// <summary>
    /// 리스트 아이템을 만드는 함수
    /// </summary>
    GuildRankListSlot GetNewObject()
    {
        GuildRankListSlot slot;
        GameObject newObj = Instantiate(guildPrefab, listParent);
        slot = newObj.GetComponent<GuildRankListSlot>();

        slotList.Add(slot);
        return slot;
    }
}
