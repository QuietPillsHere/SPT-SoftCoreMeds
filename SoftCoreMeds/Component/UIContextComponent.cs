using EFT.InventoryLogic;
using System;
using System.Collections.Generic;
using System.Text;
using static UnityEngine.EventSystems.PointerEventData;

namespace SoftCoreMeds.Component
{
    public class UIContextComponent : GClass1944, IItemComponent
    {
        public InputButton input { get; set; }

        public bool DoubleClick { get; set; }

        public bool Serialized => false;

        public UIContextComponent(Item item, InputButton input, bool DoubleClick)
        {
            this.input = input;
            this.DoubleClick = DoubleClick;
        }
    }
}
