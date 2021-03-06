﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;
using YamlDotNet.Serialization;
public delegate void IAPFinish(bool success,string id = null);

public class IAPManager : IStoreListener
{

    public static IAPManager instance = null;
    static public IAPManager Instance
    {
        get
        {
            return instance;
        }
    }
    
    static public void Init(IAPFinish callback)
    {
        if (instance == null)
        {
            instance = new IAPManager();
            instance.defaultFinishCallback = callback;
        }
    }
    private IAPFinish finishCallback = null;
    private IAPFinish defaultFinishCallback = null;
    private IStoreController controller = null;
    private IExtensionProvider extensions = null;
    private bool isRestore = false;
    InitializationFailureReason iapError;
    bool isInitializeFailed = false;
    ProductType[] productType = { ProductType.Consumable, ProductType.NonConsumable, ProductType.Subscription };
    public IAPManager()
    {
        initStore();   
    }
    void initStore(){
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
        Object obj = Resources.Load("IAP/IAPDATA");
        if (obj)
        {
            string dataAsYaml = obj.ToString();
            Deserializer deserializer = new Deserializer();
            Dictionary<string, int> dic = deserializer.Deserialize<Dictionary<string, int>>(new StringReader(dataAsYaml));
            foreach (KeyValuePair<string, int> kv in dic)
            {
                builder.AddProduct(kv.Key, productType[kv.Value]);
            }
        }
        UnityPurchasing.Initialize(this, builder);
    }
    /// <summary>
    /// Called when Unity IAP is ready to make purchases.
    /// </summary>
    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        this.controller = controller;
        this.extensions = extensions;
        isInitializeFailed = false;
//		Restore ();
    }
    
	public void Restore(){
        if(IsBusy()){
            return;
        }
		UIController.Instance.PushLoading("restoreStart","购买正在恢复中");

        isRestore = true;
		extensions.GetExtension<IAppleExtensions>().RestoreTransactions(result => {
            isRestore = false;
			UIController.Instance.Pop("restoreStart");

            string key = "恢复失败";
            if (result)
            {
                key = "恢复成功";
            }
			UIController.Instance.PushHint("restore",key);
		});
	}
    /// <summary>
    /// Called when Unity IAP encounters an unrecoverable initialization error.
    ///
    /// Note that this will not be called if Internet is unavailable; Unity IAP
    /// will attempt initialization until it becomes available.
    /// </summary>
    public void OnInitializeFailed(InitializationFailureReason error)
    {
        initStore();   
        Debug.LogWarning("QIPAWORLD:内购初始化失败");
        iapError = error;
        isInitializeFailed = true;
        switch (error)
        {
            case InitializationFailureReason.AppNotKnown:
                Debug.LogWarning("QIPAWORLD:一个未知错误，请检查后台配置");
                break;
            case InitializationFailureReason.PurchasingUnavailable:
                // Ask the user if billing is disabled in device settings.
                Debug.LogWarning("QIPAWORLD:禁止购买");
                break;
            case InitializationFailureReason.NoProductsAvailable:
                // Developer configuration error; check product metadata.
                Debug.LogWarning("QIPAWORLD:没有商品");
                break;

        }
    }
    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e)
    {
        bool validPurchase = true; // Presume valid for platforms with no R.V.
		string id = e.purchasedProduct.definition.id;
        // Unity IAP's validation logic is only included on these platforms.
#if UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE_OSX
        //Prepare the validator with the secrets we prepared in the Editor
        // obfuscation window.
        var validator = new CrossPlatformValidator(GooglePlayTangle.Data(),
            AppleTangle.Data(), Application.identifier);

        try
        {
            // On Google Play, result has a single product ID.
            // On Apple stores, receipts contain multiple products.
            var result = validator.Validate(e.purchasedProduct.receipt);
            // For informational purposes, we list the receipt(s)
            foreach (IPurchaseReceipt productReceipt in result)
            {
                Debug.Log(productReceipt.productID);
                Debug.Log(productReceipt.purchaseDate);
                Debug.Log(productReceipt.transactionID);
            }
        }
        catch (IAPSecurityException)
        {
            UIController.Instance.PushHint("IAPSecurityException","购买验证失败");
            validPurchase = false;
        }
#endif

        if (validPurchase)
        {
            SendCallBack(true, id);
        }
        else
        {
            SendCallBack(false,id);
        }

        return PurchaseProcessingResult.Complete;
    }
    public bool IsBusy(){
       
        if (controller == null || extensions == null){
            if(isInitializeFailed){
                if(iapError == InitializationFailureReason.AppNotKnown){
                    UIController.Instance.PushHint("iapError","遇到了一个未知错误商品没有初始化");
                }else if(iapError == InitializationFailureReason.PurchasingUnavailable){
                    UIController.Instance.PushHint("iapError","您似乎禁止了购买");
                }else if(iapError == InitializationFailureReason.NoProductsAvailable){
                    UIController.Instance.PushHint("iapError","开发人员配置错误没有商品");
                }
            }
            else{
                UIController.Instance.PushHint("notController","商店正在加载中");
            }
            return true;
        }
        else if(finishCallback!=null){
            UIController.Instance.PushHint("finishCallbackNotNull","购买正在进行中");
            return true;
        }
        else if(isRestore){
            UIController.Instance.PushHint("isRestore","购买正在恢复中");
            return true;
        }
        return false;
    }
    public void OnPurchaseClicked(string productId,IAPFinish callback)
    {
        if(IsBusy()){
            return;
        }
		UIController.Instance.PushLoading("purchaseStart","购买正在进行中");

        finishCallback = callback;
        controller.InitiatePurchase(productId);
    }

   
    /// <summary>
    /// Called when a purchase fails.
    /// </summary>
    public void OnPurchaseFailed(Product i, PurchaseFailureReason p)
    {
		SendCallBack(false,i.definition.id);
    }
    public void SendCallBack(bool success, string id = null)
    {
//        string key = "购买失败";
//        if (success)
//        {
//            key = "购买成功";
//        }
//		GameObject hintLayer = UIController.Instance.Push("hintLayer");

//        hintLayer.GetComponentInChildren<Text>().text = LocalizationManager.Instance.GetLocalizedValue(key);
		UIController.Instance.Pop("purchaseStart");

		if (finishCallback == null) {
			defaultFinishCallback (success, id);
		} else {
			finishCallback(success,id);
		}
        finishCallback = null;
        
    }
}
