using Beebyte.Obfuscator;
using CSVData;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class csGuildShopSlot : csShopSlot
{
    //[SerializeField] protected Text txtDailyLimit = null;

    GuildShopPage gMgr = null;
    [HideInInspector] public GuildShopProductCSV gCsv = null;

    public void Init(GuildShopPage _mgr, GuildShopProductCSV _productcsv)
    {
        CloseCurtainEffect();
        gCsv = _productcsv;
        gMgr = _mgr;

        if (gCsv != null)
        {
            gameObject.SetActive(true);
            lockObject.SetActive(false);

            txtName.text = LocalizationSystem.GetLocalizedText(gCsv.productName);
            LoadProductImage();

            __SetItemCnt();

            if (string.IsNullOrEmpty(gCsv.expectedEffect)) txtExpectedEffect.text = "";
            else txtExpectedEffect.text = LocalizationSystem.GetLocalizedText( gCsv.expectedEffect);

            UpdatePrice();


            SetBuyLimit();
            __SetPackageInfo();
            //__SetDiscountImage();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    protected override void UpdatePrice()
    {
        if (gCsv.isIap)
        {
            txtNormalPrice.text = "";
            payItemImage.gameObject.SetActive(false);

            txtIAPPrice.text = ShopManager.instance.GetIAPPrice(gCsv.iapName);
            return;
        }

        if (gCsv.payItemCnt == 0 || gCsv.payItemId == 0)
        {
            payItemImage.gameObject.SetActive(false);
            txtNormalPrice.text = "";
            txtIAPPrice.text = MessageContainer.Free;
            return;
        }

        txtIAPPrice.text = "";
        payItemImage.gameObject.SetActive(true);
        payItemImage.sprite = ResourceManager.GetInstance().GetRewardSprite((int)RewardType.ITEM, gCsv.payItemId);
        txtNormalPrice.text = string.Format(MessageContainer.CountFormat, ResourceConverter.GetUnitExpression(gCsv.payItemCnt));
    }

    protected override void __SetDiscountImage()
    {
        if (gCsv.id == 7)
        {
            GameObject obj = Instantiate(discountPrefab, discountParent);
            obj.SetActive(true);
        }
    }

    protected override void LoadProductImage()
    {
        Sprite sprite;
        if (gCsv.productImageName == null || gCsv.productImageName.Equals(""))
        {
            sprite = ResourceManager.GetInstance().GetRewardSprite(gCsv.rewardTypes[0], gCsv.rewardRefIds[0]);
        }
        else
        {
            sprite = ResourceManager.GetInstance().GetShopProductImage(gCsv.productImageName);

        }

        productImage.sprite = sprite;
    }

    public override void __SetPackageInfo()
    {
        
        if (!string.IsNullOrEmpty(gCsv.additionalInfo))
        {
            packageProductObject.SetActive(true);
            txtCnt.gameObject.SetActive(false);

            txtPackagePordcut.text = LocalizationSystem.GetLocalizedText(gCsv.additionalInfo);
            return;
        }

        bool isPackageItem = gCsv.shopId == (int)GuildShopCategoryEnum.Skin;


        packageProductObject.SetActive(isPackageItem);
        txtCnt.gameObject.SetActive(!isPackageItem);

        if (isPackageItem)
        {
            List<Source> srcs = CSVParser.FindGuildSourceFormIAP(gCsv.iapName);
            if (srcs == null) return;

            StringBuilder builder = new StringBuilder();

            int length = srcs.Count;
            for (int i = 0; i < length; i++)
            {
                builder.Append(CSVFactory.FindRewardName(srcs[i].itemType, srcs[i].itemId) + " ");
                builder.AppendFormat(MessageContainer.CountFormat,  ResourceConverter.GetUnitExpression(srcs[i].ItemCnt));

                if (i != length - 1) builder.Append("\n");
            }

            txtPackagePordcut.text = builder.ToString();
        }
    }

    public override void SetBuyLimit()
    {
        int buyCnt = UserData.ShopDB.GetGuildShopBuyCnt(gCsv.id);

        if (buyCnt > 0) FirstBuyObject.SetActive(false);
        else
        {
            if (gCsv.firstPurchaseBonusRatio == 0) FirstBuyObject.SetActive(false);
            else
            {
                FirstBuyObject.SetActive(true);
                txtFirstBuy.text = string.Format(MessageContainer.RatioFormat, gCsv.firstPurchaseBonusRatio);
            }
        }

        //bool isCompletedPuchase = false;

        //for(int i=0; i < gCsv.rewardTypes.Length; i++)
        //{
        //    if (gCsv.rewardTypes[i] == (int)RewardType.ITEM)
        //    {
        //        ItemCSV itemcsv = CSVParser.GetItemCSV(gCsv.rewardRefIds[i]);
        //        if (itemcsv.maxValue == 1 && UserData.instance.GetCount(gCsv.rewardTypes[i], gCsv.rewardRefIds[i]) >= 1)
        //        {
        //            isCompletedPuchase = true;
        //        }
        //    }
        //    else if (gCsv.rewardTypes[i] == (int)RewardType.CHARACTER_SKIN)
        //    {
        //        if (UserData.SkinDB.HasSkin(SkinType.CHARACTER, gCsv.rewardRefIds[i]))
        //        {
        //            isCompletedPuchase = true;
        //        }
        //    }
        //    else if (gCsv.rewardTypes[i] == (int)RewardType.WEAPON_SKIN)
        //    {
        //        if (UserData.SkinDB.HasSkin(SkinType.WEAPON, gCsv.rewardRefIds[i]))
        //        {
        //            isCompletedPuchase = true;
        //        }
        //    }
        //}


        //if (isCompletedPuchase)
        //{
        //    txtLimit.text = "";
        //    SetCompleteBuy();
        //    return;
        //}

        int buyDailyCnt = UserData.ShopDB.GetGuildShopDailyBuyCnt(gCsv.id);

        //???? ???? ?????? ????
        if (gCsv.dailyBuyLimit != 0)
        {
            if (gCsv.dailyBuyLimit <= buyDailyCnt)
            {
                SetCompleteBuy();
                txtLimit.text = LocalizationSystem.GetLocalizedText("successdailypurchase");
                return;
            }

            txtLimit.text = string.Format(MessageContainer.DailyPurchaseLimit + "{0}/{1}", gCsv.dailyBuyLimit - buyDailyCnt, gCsv.dailyBuyLimit);
            buyBtnBackground.sprite = normalBuySprite;
        }
        //???? ???? ?????? ???? ????
        else
        {
            if (gCsv.buyLimitCnt == 0)
            {
                txtLimit.text = "";
                lockObject.SetActive(false);
                buyBtnBackground.sprite = normalBuySprite;
            }
            else
            {
                txtLimit.text = string.Format(MessageContainer.PurchaseLimit + ": {0}/{1}", gCsv.buyLimitCnt - buyCnt, gCsv.buyLimitCnt);
                
                bool isBought = false;

                if (AleardyBuyProduct(buyCnt)) isBought = true;


                if (isBought)
                {
                    if (CSVParser.ExistNextGuildProduct(gCsv.id))
                    {
                        OpenNextProductEffect();
                    }
                    else
                    {
                        SetCompleteBuy();
                    }
                }
                else
                {
                    lockObject.SetActive(false);
                    buyBtnBackground.sprite = normalBuySprite;
                }
            }
        }
    }

    protected override bool AleardyBuyProduct(int userBuyCnt)
    {
        if (gCsv.buyLimitCnt > 0 && userBuyCnt >= gCsv.buyLimitCnt) return true;

        for(int i = 0; i < gCsv.rewardTypes.Length; i++)
        {
            if (gCsv.rewardTypes[i] == (int)RewardType.ITEM)
            {
                ItemCSV item = CSVParser.GetItemCSV(gCsv.rewardRefIds[i]);
                long userCount = UserData.instance.GetCount(RewardType.ITEM, item.id);
                if (userCount >= item.maxValue) return true;
            }
        }

        return false;
    }

    protected override IEnumerator IShowNextProduct()
    {
        gCsv = CSVParser.GetNextGuildProductCSV(gCsv.id);
        if (gCsv == null) yield break;

        productUpgradeProcessing = true;
        showNextProductAnimator.SetActive(true);
        yield return new WaitForSeconds(1.2f);

        Init(gMgr, gCsv);
        yield return new WaitForSeconds(1f);

        CloseCurtainEffect();
    }

    protected override void __SetItemCnt()
    {
        //???????? ?????? 1???? ?????? ???? ????.
        if (gCsv.rewardItemCnts.Length <= 1)
        {
                //???? ???? ?????? ?????????? ???? ???????? ???? ????.
                double totalCnt = gCsv.rewardItemCnts[0] + Math.Floor(gCsv.incRewardItemCnts[0] * (GuildManager.instance.guildLv-1));
                txtCnt.text = $"x{ResourceConverter.GetUnitExpression(totalCnt)}";
        }
        else
            txtCnt.text = "";

    }


    [SkipRename]
    public void ClickGShopIcon()
    {
        int buyCnt = UserData.ShopDB.GetGuildShopBuyCnt(gCsv.id);
        int buyDailyCnt = UserData.ShopDB.GetGuildShopDailyBuyCnt(gCsv.id);

        if(gCsv.dailyBuyLimit != 0 && gCsv.dailyBuyLimit <= buyDailyCnt)
        {
            PostAlarmManager.instance.CreateAlarmPost(MessageContainer.ExceedPurchaseLimit);
            return;
        }

        if (gCsv.buyLimitCnt != 0 && gCsv.buyLimitCnt <= buyCnt)
        {
            PostAlarmManager.instance.CreateAlarmPost(MessageContainer.ExceedPurchaseLimit);
            return;
        }

        gMgr.ShopBuy(this);
    }
}
