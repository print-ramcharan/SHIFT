using System;
using System.Collections.Generic;
using UnityEngine;

// Unity IAP — only active when the package is imported
#if UNITY_PURCHASING
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
#endif

/// <summary>
/// Handles in-app purchases for SHIFT using Unity IAP.
/// Products: three Shard bundles + Creator Pass subscription.
///
/// SETUP:
///   1. Window → Package Manager → Unity Registry → In App Purchasing → Install
///   2. Add UNITY_PURCHASING to Scripting Define Symbols (Player Settings)
///   3. Enable IAP in Services window (Window → General → Services)
///
/// Without UNITY_PURCHASING defined, all buy methods log warnings — game runs safely.
/// </summary>
#if UNITY_PURCHASING
public class IAPManager : MonoBehaviour, IDetailedStoreListener
#else
public class IAPManager : MonoBehaviour
#endif
{
    public static IAPManager Instance { get; private set; }

    // ─── Events ──────────────────────────────────────────────────────────────────
    public static event Action<string> OnPurchaseSuccess;
    public static event Action<string> OnPurchaseFailed;

    // ─── Shard amounts per product ───────────────────────────────────────────────
    private static readonly Dictionary<string, int> ShardAmounts = new Dictionary<string, int>
    {
        { Constants.IAP_SHARDS_SMALL,  100 },
        { Constants.IAP_SHARDS_MEDIUM, 300 },
        { Constants.IAP_SHARDS_LARGE,  750 }
    };

#if UNITY_PURCHASING
    private IStoreController _storeController;
    private IExtensionProvider _extensions;
#endif

    // ─── Lifecycle ───────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        InitialisePurchasing();
    }

    // ─── Initialisation ──────────────────────────────────────────────────────────

    private void InitialisePurchasing()
    {
#if UNITY_PURCHASING
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

        // Non-consumable: Creator Pass
        builder.AddProduct(Constants.IAP_CREATOR_PASS,  ProductType.NonConsumable);

        // Consumable: Shard bundles
        builder.AddProduct(Constants.IAP_SHARDS_SMALL,  ProductType.Consumable);
        builder.AddProduct(Constants.IAP_SHARDS_MEDIUM, ProductType.Consumable);
        builder.AddProduct(Constants.IAP_SHARDS_LARGE,  ProductType.Consumable);

        UnityPurchasing.Initialize(this, builder);
        Debug.Log("[IAP] Initialising Unity IAP...");
#else
        Debug.LogWarning("[IAP] UNITY_PURCHASING not defined — IAP disabled.");
#endif
    }

    // ─── Public Buy API ──────────────────────────────────────────────────────────

    public void BuyShardsSmall()  => BuyProduct(Constants.IAP_SHARDS_SMALL);
    public void BuyShardsMedium() => BuyProduct(Constants.IAP_SHARDS_MEDIUM);
    public void BuyShardsLarge()  => BuyProduct(Constants.IAP_SHARDS_LARGE);
    public void BuyCreatorPass()  => BuyProduct(Constants.IAP_CREATOR_PASS);

    private void BuyProduct(string productId)
    {
#if UNITY_PURCHASING
        if (_storeController == null)
        {
            Debug.LogWarning("[IAP] Store not initialised yet.");
            return;
        }
        _storeController.InitiatePurchase(productId);
#else
        Debug.LogWarning($"[IAP] Cannot purchase {productId} — UNITY_PURCHASING not defined.");
#endif
    }

    // ─── Unity IAP Callbacks ─────────────────────────────────────────────────────

#if UNITY_PURCHASING
    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        _storeController = controller;
        _extensions = extensions;
        Debug.Log("[IAP] Initialised successfully.");
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.LogError($"[IAP] Init failed: {error}");
    }

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        Debug.LogError($"[IAP] Init failed: {error} — {message}");
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        string productId = args.purchasedProduct.definition.id;
        Debug.Log($"[IAP] Purchase complete: {productId}");

        // Grant shards for shard packs
        if (ShardAmounts.TryGetValue(productId, out int shards))
        {
            RewardSystem.Instance?.GrantShards(shards);
            Debug.Log($"[IAP] Granted {shards} shards for {productId}");
        }

        // Creator Pass — unlock flag
        if (productId == Constants.IAP_CREATOR_PASS)
        {
            PlayerPrefs.SetInt("creator_pass_owned", 1);
            PlayerPrefs.Save();
            Debug.Log("[IAP] Creator Pass unlocked.");
        }

        OnPurchaseSuccess?.Invoke(productId);
        return PurchaseProcessingResult.Complete;
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        Debug.LogWarning($"[IAP] Purchase failed: {product.definition.id} — {failureReason}");
        OnPurchaseFailed?.Invoke(product.definition.id);
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
    {
        Debug.LogWarning($"[IAP] Purchase failed: {product.definition.id} — {failureDescription.message}");
        OnPurchaseFailed?.Invoke(product.definition.id);
    }
#endif

    // ─── Restore (iOS required) ───────────────────────────────────────────────────

    public void RestorePurchases()
    {
#if UNITY_PURCHASING && UNITY_IOS
        var apple = _extensions?.GetExtension<IAppleExtensions>();
        apple?.RestoreTransactions(result =>
        {
            Debug.Log(result ? "[IAP] Restore success." : "[IAP] Restore failed.");
        });
#else
        Debug.Log("[IAP] Restore not needed on this platform.");
#endif
    }
}
