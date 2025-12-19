using UnityEngine;
using TMPro; // 引入TextMeshPro命名空间

public class AttributeUIManager : MonoBehaviour
{
    [Header("关联UI文本组件")]
    [SerializeField] private TextMeshProUGUI textHp;       // 显示血量的Text
    [SerializeField] private TextMeshProUGUI textAttack;   // 显示攻击的Text
    [SerializeField] private TextMeshProUGUI textDefense;  // 显示防御的Text
    [SerializeField] private TextMeshProUGUI textGold;     // 显示金币的Text

    private PlayerAttribute _playerAttribute;


    private void Awake()
    {
        // 获取实例
        _playerAttribute = PlayerAttribute.Instance;
        if (_playerAttribute == null)
        {
            Debug.LogError("场景中找不到PlayerAttribute实例！");
            return;
        }
    }
    private void OnEnable()
    {
        // 改为订阅事件中心的属性变化事件
        EventCenter.Instance.OnAttributeChanged += OnAttributeChanged;
    }

    private void OnDisable()
    {
        // 取消订阅
        EventCenter.Instance.OnAttributeChanged -= OnAttributeChanged;
    }
    private void OnAttributeChanged(object sender, AttributeChangedEventArgs args)
    {
        UpdateUI(args.ChangedType);
    }


    // 【核心】更新所有属性的UI显示
    private void UpdateUI(AttributeType type)
    {
        switch (type)
        {
            case AttributeType.All: // 全量更新（所有UI）
                UpdateHpUI();
                UpdateAttackUI();
                UpdateDefenseUI();
                UpdateGoldUI();
                break;
            case AttributeType.HP: // 仅更新血量UI
                UpdateHpUI();
                break;
            
            case AttributeType.Attack: // 仅更新攻击UI
                UpdateAttackUI();
                break;
            
            case AttributeType.Defense: // 仅更新防御UI
                UpdateDefenseUI();
                break;
            case AttributeType.Gold:
                UpdateGoldUI();
                break;
            default:
                break;
        }
    }
    //单个UI的独立更新方法
    private void UpdateHpUI()
    {
        // 把“当前HP/最大HP”赋值给textHp文本
        textHp.text = $"{_playerAttribute.CurrentHP}";
    }

    private void UpdateAttackUI()
    {
        // 把“攻击数值”赋值给textAttack文本
        textAttack.text = $"{_playerAttribute.Attack}";
    }

    private void UpdateDefenseUI()
    {
        // 把“防御数值”赋值给textDefense文本
        textDefense.text = $"{_playerAttribute.Defense}";
    }
    private void UpdateGoldUI()
    {
        // 把“防御数值”赋值给textDefense文本
        textGold.text = $"{_playerAttribute.Gold}";
    }
}