using Beebyte.Obfuscator;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GuildRandListPage : MonoBehaviour
{
    [SerializeField] float ITEM_HEIGHT = 0F;
    [SerializeField] GameObject guildPrefab = null;
    [SerializeField] RectTransform listParent = null;
    [SerializeField] GameObject txtHaveNotGuild = null;
    

    List<GuildRandListSlot> slotList = new List<GuildRandListSlot>();
    public List<GuildInfo> guildRandInfos = null;

    public GuildListPage mgr;
    const int showCnt = 10;
    public float height = 0f;

    public void OpenPage(GuildListPage _mgr)
    {
        if (guildPrefab.activeSelf) guildPrefab.SetActive(false);

        mgr = _mgr;

        GetRandGuildInfo();
    }

    /// <summary>
    /// �ڳ��� ����� ��� ������ �������� �����´�(20��).
    /// </summary>
    void GetRandGuildInfo()
    {
        //���������Ͱ� ���� ��� �����ϰ� ���ο� ������� ���.
        if (guildRandInfos != null && guildRandInfos.Count != 0)
        {
            guildRandInfos.Clear();
            height = 0f;
        }

        mgr.loadingImg.SetActive(true);
        IdleBackend.instance.GetRandGuildList(showCnt, OnCompleteAction, onFailed);
    }

    void OnCompleteAction(List<GuildInfo> result)
    {
        guildRandInfos = result;

        //������ ���� ��� ����
        for (int i = 0; i < guildRandInfos.Count; i++)
        {
            if (guildRandInfos[i] == null || guildRandInfos[i].memberCount == 0) guildRandInfos.Remove(guildRandInfos[i]);
        }

        //����� ��� ������ ���� ��� ó��.
        if (guildRandInfos == null || guildRandInfos.Count == 0)
        {
            txtHaveNotGuild.SetActive(true);

            if (slotList.Count > 0)
            {
                foreach (var item in slotList)
                {
                    item.gameObject.SetActive(false);
                }

                slotList.Clear();
            }
        }
        //����� ��� ������ guildPrefab���� ����
        else
        {
            txtHaveNotGuild.SetActive(false);
            RelocateItems();
        }
        mgr.loadingImg.SetActive(false);
    }

    void onFailed(string msg)
    {
        if(!string.IsNullOrEmpty(msg))
            PostAlarmManager.instance.CreateAlarmPost(msg);

        mgr.loadingImg.SetActive(false);
    }

    /// <summary>
    /// ����� ���� ���� ����Ʈ �������� ���� �� ������ �Ľ��� ���ִ� �Լ�.
    /// </summary>
    public void RelocateItems()
    {
        int length = guildRandInfos.Count;
        GuildRandListSlot slot = null;
        
        for (int i = 0; i < length; i++)
        {
            //��帮��Ʈ�� ������ ������ ���� ���� ��� ������ �߰��� ����.
            if (slotList.Count <= i) slot = GetNewObject();
            else slot = slotList[i];

            if (GuildManager.instance.myGuildInfo != null)
                slot.btnApply.SetActive(false);
            else
                slot.btnApply.SetActive(true);

            //�������� ������ ������� ����Ʈ�� i��° �迭�� GuildUISlot�� ������ �Ľ�.
            slot.UpdateItem(guildRandInfos[i], this);
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
    GuildRandListSlot GetNewObject()
    {

        GuildRandListSlot slot;
        GameObject newObj = Instantiate(guildPrefab, listParent);
        slot = newObj.GetComponent<GuildRandListSlot>();

        slotList.Add(slot);
        return slot;
    }
}
