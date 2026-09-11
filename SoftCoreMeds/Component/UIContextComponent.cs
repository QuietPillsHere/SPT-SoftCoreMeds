using EFT.InventoryLogic;
using System;
using System.Collections.Generic;
using System.Text;
using static UnityEngine.EventSystems.PointerEventData;

namespace SoftCoreMeds.Component
{
    public class UIContextComponent : IItemComponent
    {
        public InputButton input { get; set; }

        public bool DoubleClick { get; set; }

        public EItemInfoButton ConsumMethod { get; set; }

        public bool Serialized => false;

        //public UIContextComponent(InputButton input, bool DoubleClick = false, EItemInfoButton? consumMethod = null)
        //{
        //    this.input = input;
        //    this.DoubleClick = DoubleClick;
        //}
    }
}
