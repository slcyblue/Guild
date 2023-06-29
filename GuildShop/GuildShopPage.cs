using Beebyte.Obfuscator;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuildShopPage : ShopPage
{

    #region Variables
    
    [Header("SHOP Categories")]
    [SerializeField] protected GuildShopGroup guildShopGroup = null;
    [SerializeField] protected ResourceUpdater additionalResourceUpdater = null;
    GuildShopCategoryEnum gSelectcategory = GuildShopCategoryEnum.NONE;

    GuildShopProductCSV crrGuildProduct = null;
    csShopSlot gCrrSlot = null;

    #endregion

    //string CannotBuyDuringContents;
    //string CannotBuyDuringBossMode;
    //string ReloadShopMsg;
    //string CancelBossDuringInapp;
    public override void Initialize()
    {
        InitCategory();
        //CannotBuyDuringContents = LocalizationSystem.GetLocalizedText("cannotbuyincontents");
        //CannotBuyDuringBossMode = LocalizationSystem.GetLocalizedText("cannotbuyinboss");
        //ReloadShopMsg = LocalizationSystem.GetLocalizedText("reloadshopmsg");
        //CancelBossDuringInapp = LocalizationSystem.GetLocalizedText("cancelbossmsg");
        ChangeCategory(GuildShopCategoryEnum.Guild);
    }

    protected override void InitCategory()
    {
        Dictionary<int, GuildShopCategoryCSV> list = CSVParser.guildShopCategory;
        int index = 0;
        foreach (var item in list)
        {
            if (index >= categories.Length) break;
            if (IsExpireShop(item.Value)) continue;

            string shopName = LocalizationSystem.GetLocalizedText(item.Value.shopName);
            categories[index].Init(item.Key, shopName, index, CategoryBtnClick);
            index++;
        }
        for (; index < categories.Length; index++)
        {
            categories[index].Hide();
        }
    }

    bool IsExpireShop(GuildShopCategoryCSV shop)
    {
        if (string.IsNullOrEmpty(shop.startDate) || string.IsNullOrEmpty(shop.endDate)) return false;

        DateTime crrDate = SessionManager.instance.GetCurrentDateTime();
        if (crrDate < DateTime.Parse(shop.startDate)) return true;
        if (DateTime.Parse(shop.endDate) < crrDate) return true;

        return false;
    }

    public override void ClosePage()
    {
        guildShopGroup.StopUpgrade();
        gameObject.SetActive(false);
        StopAllCoroutines();
    }

    public override void OpenPage()
    {
        gameObject.SetActive(true);
        FocusShopItem();
        UpdateResource();
        StartCoroutine(IDailyTimer());

    }

    [SkipRename]
    public override void ClickBackBtn()
    {
        GameUIManager.pageSwapper.SwitchPanel(UIPanelEnum.GuildHome);
        SoundManager.instance.PlaySFX(SoundEnum.uiClickBtn);
    }

    public override void CategoryBtnClick(int index)
    {
        FocusShopItem((GuildShopCategoryEnum)categories[index].Key, -1, -1);
    }

    void SetSpriteOnCategory(GuildShopCategoryEnum shopid, Sprite sprite)
    {
        int length = categories.Length;
        for (int i = 0; i < length; i++)
        {
            if (categories[i].Key == (int)shopid)
            {
                categories[i].SetSprite(sprite);
            }
        }
    }

    public void FocusShopItem(GuildShopCategoryEnum category = GuildShopCategoryEnum.Guild, int rewardType = -1, int rewardId = -1)
    {
        ChangeCategory(category);
        //TODO
    }

    [SkipRename]
    public void ChangeCategory(GuildShopCategoryEnum newCatgory)
    {
        if (gSelectcategory != GuildShopCategoryEnum.NONE) SetSpriteOnCategory(gSelectcategory, unselectedSprite);
        if (newCatgory != GuildShopCategoryEnum.NONE) SetSpriteOnCategory(newCatgory, selectedSprite);

        gSelectcategory = newCatgory;

        guildShopGroup.Init(gSelectcategory);
        shopListParent.sizeDelta = Vector3.up * guildShopGroup.GetHeight();
        //UpdateAdditionalResources();
    }

    public void ShopBuy(csGuildShopSlot _slot)
    {
        if (!_slot)
        {
            return;
        }
        crrGuildProduct = _slot.gCsv;
        gCrrSlot = _slot;
        /*
#if UNITY_IOS
        if (crrGuildProduct.isIap == true && LoginManager.isGuestLogin)
        {
            //게스트 계정은 인앱상품을 구매할 수 없습니. 계정연동을 진행해주세.
            AlarmPopupManager.instance.OpenAlarm(LocalizationSystem.GetLocalizedText("guestbuyerror"));
            return;
        }
#endif
        */

        if (ShopManager.instance == null)
        {
            PostAlarmManager.instance.CreateAlarmPost(MessageContainer.CommonServerError);
            return;
        }

        ContentStage stageMode = GameManager.instance.playCtr as ContentStage;
        if (stageMode == null)
        {
            PostAlarmManager.instance.CreateAlarmPost(CannotBuyDuringContents);
            return;
        }

        if (crrGuildProduct.isIap)
        {

            if (stageMode.IsPlayingBossNow())
            {
                PostAlarmManager.instance.CreateAlarmPost(CannotBuyDuringBossMode);
                return;
            }
            else
            {
                if (ContentStage.IsAutoBossOn)
                {
                    stageMode.StopAutoBossSystem();
                    PostAlarmManager.instance.CreateAlarmPost(CancelBossDuringInapp);
                }
            }
        }

        long[] temp = new long[crrGuildProduct.rewardItemCnts.Length];

        for(int i=0; i< crrGuildProduct.rewardItemCnts.Length; i++)
        {
            temp[i] = crrGuildProduct.rewardItemCnts[i] + (int)Math.Floor(crrGuildProduct.incRewardItemCnts[i] * (GuildManager.instance.guildLv-1));
        }

        ShopManager.instance.StartPurchase(crrGuildProduct, temp ,OnCompletePurchase, base.OnFailPurchase);

    }

    protected override void OnCompletePurchase()
    {
        PostAlarmManager.instance.CreateAlarmPost(MessageContainer.SuccessPurchase);

        //if (crrGuildProduct.shopId == (int)GuildShopCategoryEnum.Pass) SoundManager.instance.PlaySFX(SoundEnum.uiPurchasePass);
        gCrrSlot.SetBuyLimit();
        UpdateResource();
    }

#region etc
    protected override void UpdateResource()
    {
        mileageUpdater.UpdateResource((int)RewardType.ITEM, (int)ItemEnum.GuildCoin, UserData.ItemDB.GetCount(ItemEnum.GuildCoin));
        additionalResourceUpdater.UpdateResource((int)RewardType.ITEM, (int)ItemEnum.GuildEntrance, UserData.ItemDB.GetCount(ItemEnum.GuildEntrance));
    }
#endregion
}
