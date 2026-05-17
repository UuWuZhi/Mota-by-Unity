using Modules.Item.DataDefine;

namespace Modules.EventSystem.DataDefine.EventArgs
{
    public class InventoryChangedEventArgs : System.EventArgs
    {
        public InventoryChangedEventArgs()
        {
        }

        public InventoryChangedEventArgs(ItemType type, int delta = 0)
        {
            ChangedType = type;
            Delta = delta;
        }

        // ���ֵ��߷����仯��ʹ�� ItemType.All ��ʾȫ������
        public ItemType ChangedType { get; set; }

        // ��ѡ�������仯����Ϊ���ӣ���Ϊ���٣������÷���ѡ�ṩ
        public int Delta { get; set; }
    }
}