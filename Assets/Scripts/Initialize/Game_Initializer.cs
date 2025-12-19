using UnityEngine;

public class GameInitializer : MonoBehaviour
{
    [Header("核心引用")]
    [SerializeField] private GameObject player; // 仅需拖入Player对象（无需其他手动设置）
    private GridManager _gridManager;       // 缓存GridManager单例
    private MapManager _mapManager;
    private PlayerInventory _playerInventory; // 背包管理器单例
    private PlayerAttribute _playerAttribute; //玩家属性单例
    private EventCenter _eventCenter;
    private void Awake()
    {
        // 1. 获取单例
        _gridManager = GridManager.Instance;
        if (_gridManager == null)
        {
            Debug.LogError("场景中未找到GridManager单例！");
            return;
        }
        _mapManager = MapManager.Instance;
        if (_mapManager == null)
        {
            Debug.LogError("场景中未找到MapManager单例！");
            return;
        }
        _playerInventory = PlayerInventory.Instance;
        if (_playerInventory == null)
        {
            Debug.LogError("场景中未找到PlayerInventory单例！");
            return;
        }
        _playerAttribute = PlayerAttribute.Instance;
        if (_playerAttribute == null)
        {
            Debug.LogError("场景中未找到PlayerAttribute单例！");
            return;
        }
        _eventCenter = EventCenter.Instance;
        if (_eventCenter == null)
        {
            Debug.LogError("场景中未找到EventCenter单例！");
            return;
        }
    }

    private void Start()
    {
        // 1. 先加载地图数据
        _mapManager.GlobalMapLoad();
        Debug.Log("地图数据加载完成！");
        // 2. 初始化玩家属性
        _playerAttribute.ResetAttribute();
        Debug.Log("玩家属性初始化完成！");

        // 3. 初始化背包道具
        _playerInventory.InitItemCounts();
        Debug.Log("背包道具初始化完成！");

        // 4. 初始化动画速度（直接读取Player和GridManager的参数）
        InitAnimationSpeed();
        Debug.Log("动画速度初始化完成！");
        _eventCenter.TriggerLayerSwitchRequest(new LayerSwitchRequestEventArgs
        {
            TargetLayerId = 1,
            SpawnPointId = 0
        });
    }

    // 核心方法：读取统一参数，计算动画速度
    private void InitAnimationSpeed()
    {
        Animator playerAnimator = player.GetComponent<Animator>();
        if (playerAnimator == null)
        {
            Debug.LogError("Player对象缺少Animator组件！");
            return;
        }

        // 动画时长已和移动时长匹配，速度设为1（正常播放）
        playerAnimator.speed = 1f;
    }
}