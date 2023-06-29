using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuildShopGroup : ShopGroup
{
    [SerializeField] protected GuildShopPage guildShopPage = null;

    GuildShopCategoryEnum m_gShopCategory = 0;
    Dictionary<int, csGuildShopSlot> activeGuildShops = new Dictionary<int, csGuildShopSlot>();
    Queue<csGuildShopSlot> guildShopPools = new Queue<csGuildShopSlot>();
    public void Init(GuildShopCategoryEnum _category)
    {
        iconPrefab.SetActive(false);
        ReturnAllShopItems();
        gameObject.SetActive(true);
        m_gShopCategory = _category;
        MakeShopCatagory();
        MakeShopItem();
    }

    protected override void ReturnAllShopItems()
    {
        foreach (var item in activeGuildShops)
        {
            guildShopPools.Enqueue(item.Value);
        }

        activeGuildShops.Clear();
    }

    public override void MakeShopCatagory()
    {
        GuildShopCategoryCSV shopCsv = CSVParser.GetGuildShopCatagory((int)m_gShopCategory);

        txtShopName.text = LocalizationSystem.GetLocalizedText( shopCsv?.shopName);

        if (string.IsNullOrEmpty(shopCsv.description))
        {
            titleInfoObject.gameObject.SetActive(false);
        }
        else
        {
            titleInfoObject.gameObject.SetActive(true);
            titleInfoObject.text = LocalizationSystem.GetLocalizedText( shopCsv.description);

            Color color = Color.white;
            ColorUtility.TryParseHtmlString(shopCsv.descriptionColor, out color);
            titleInfoObject.color = color;
        }
    }

    public override void MakeShopItem()
    {
        float yPos = INIT_Y;
        float xPos = INIT_X + INIT_X;
        int itemCount = 0;                  // 3칸 단위로 전시하기위해 사용

        Dictionary<int, GuildShopProductCSV> shopProduct = CSVParser.GetGuildShopCatagorySaleProducts((int)m_gShopCategory);

        if (shopProduct.Count == 0) activeGuildShops.Clear();

        foreach (var prodcut in shopProduct)
        {
            if (!IsShowableItem(prodcut.Value)) continue;
            
            // 위치 설정
            if (itemCount == 3) // 3개면 초기화.
            {
                yPos -= HEIGHT;
                xPos = INIT_X;
                itemCount = 0;
            }
            else
            {
                xPos += WIDTH;
            }

            csGuildShopSlot shopItem = GetGuildShopObject();

            shopItem.Init(guildShopPage, prodcut.Value);
            shopItem.SetPosition(xPos, yPos);
            activeGuildShops.Add(prodcut.Key, shopItem);       // prodcut.Key => id
            itemCount++;
        }

        HideInactiveItems();

        shopItemListParent.sizeDelta = new Vector3(0, (-1) * (yPos - (HEIGHT * 2)), 0);
        rectBackground.sizeDelta = new Vector3(0, ((-1) * (yPos - (HEIGHT * 2))), 0);
    }

    bool IsShowableItem(GuildShopProductCSV csv)
    {
        //int currentUserStage = UserData.accountDB.LastClearStage + 1;
        //if (csv.stageLimit > currentUserStage) return false;

        if (!string.IsNullOrEmpty(csv.startDate) && !string.IsNullOrEmpty(csv.endDate))
        {
            DateTime crrDateTime = SessionManager.instance.GetCurrentDateTime();
            
            if (DateTime.Parse(csv.startDate) > crrDateTime) return false;
            if (DateTime.Parse(csv.endDate) < crrDateTime) return false;
        }

        if (csv.condShopProductId != 0)
        {
            if (UserData.ShopDB.GetGuildShopBuyCnt(csv.condShopProductId) == 0) return false;
        }

        int buyCnt = UserData.ShopDB.GetGuildShopBuyCnt(csv.id);
        if (csv.buyLimitCnt != 0)
        {
            if (buyCnt >= csv.buyLimitCnt) return false;
        }

        return true;
    }

    csGuildShopSlot GetGuildShopObject()
    {
        if (guildShopPools.Count > 0) return guildShopPools.Dequeue();
        GameObject newObj = Instantiate(iconPrefab, shopItemListParent);
        return newObj.GetComponent<csGuildShopSlot>();
    }

    protected override void HideInactiveItems()
    {
        foreach (var item in guildShopPools)
        {
            item.gameObject.SetActive(false);
        }
    }

    public new float GetHeight()
    {
        return shopItemListParent.sizeDelta.y;
    }

    public new void StopUpgrade()
    {
        foreach (var item in activeGuildShops)
        {

            item.Value.CheckCoroutine();
        }
    }
}
