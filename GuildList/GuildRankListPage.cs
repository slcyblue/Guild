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
    /// �ڳ��� ����� ��� ��ŷ ������ �����´�(20��,����ġ��).
    /// </summary>
    void GetGuildRankInfo()
    {
        //���������Ͱ� ���� ��� �����ϰ� ���ο� ������� ���.
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

        //����� ��� ������ ���� ��� ó��.
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
        //����� ��� ������ guildPrefab���� ����
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
    /// ����� ���� ���� ����Ʈ �������� ���� �� ������ �Ľ��� ���ִ� �Լ�.
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
    /// ����Ʈ �������� ����� �Լ�
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
