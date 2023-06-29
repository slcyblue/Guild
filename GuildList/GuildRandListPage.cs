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
    /// 뒤끝에 저장된 길드 정보를 랜덤으로 가져온다(20개).
    /// </summary>
    void GetRandGuildInfo()
    {
        //기존데이터가 있을 경우 삭제하고 새로운 길드목록을 출력.
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

        //정보가 없는 길드 제거
        for (int i = 0; i < guildRandInfos.Count; i++)
        {
            if (guildRandInfos[i] == null || guildRandInfos[i].memberCount == 0) guildRandInfos.Remove(guildRandInfos[i]);
        }

        //추출된 길드 정보가 없을 경우 처리.
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
        //추출된 길드 정보를 guildPrefab으로 생성
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
    /// 길드목록 수에 따라 리스트 아이템을 생성 및 데이터 파싱을 해주는 함수.
    /// </summary>
    public void RelocateItems()
    {
        int length = guildRandInfos.Count;
        GuildRandListSlot slot = null;
        
        for (int i = 0; i < length; i++)
        {
            //길드리스트의 수보다 슬롯의 수가 적을 경우 슬롯을 추가로 생성.
            if (slotList.Count <= i) slot = GetNewObject();
            else slot = slotList[i];

            if (GuildManager.instance.myGuildInfo != null)
                slot.btnApply.SetActive(false);
            else
                slot.btnApply.SetActive(true);

            //랜덤으로 추출한 길드정보 리스트의 i번째 배열로 GuildUISlot에 데이터 파싱.
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
    /// 리스트 아이템을 만드는 함수
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
