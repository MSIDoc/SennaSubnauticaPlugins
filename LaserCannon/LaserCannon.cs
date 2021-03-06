﻿using System.Collections.Generic;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;

namespace LaserCannon
{
    internal class LaserCannon : Craftable
    {
        public static TechType TechTypeID { get; private set; }        
        public static Options Config { get; } = new Options();       

        internal LaserCannon()
            : base(nameID: "LaserCannon",
                  friendlyName: global::LaserCannon.Config.language_settings["Item_Name"].ToString(),
                  description: global::LaserCannon.Config.language_settings["Item_Description"].ToString(),
                  template: TechType.SeamothSonarModule,
                  fabricatorType: CraftTree.Type.SeamothUpgrades,
                  fabricatorTab: "SeamothModules",
                  requiredAnalysis: TechType.BaseUpgradeConsole,
                  groupForPDA: TechGroup.VehicleUpgrades,
                  categoryForPDA: TechCategory.VehicleUpgrades)
        {
        }
               
        public override void Patch()
        {
            base.Patch();
            CraftDataHandler.SetEquipmentType(TechType, EquipmentType.VehicleModule);
            CraftDataHandler.SetQuickSlotType(TechType, QuickSlotType.Selectable);            
            TechTypeID = TechType;           
            OptionsPanelHandler.RegisterModOptions(Config);
        }
        
        protected override TechData GetRecipe()
        {
            return new TechData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>(new Ingredient[4]
                {
                    new Ingredient(TechType.RepulsionCannon, 1),
                    new Ingredient(TechType.PropulsionCannon, 1),
                    new Ingredient(TechType.PowerCell, 2),
                    new Ingredient(TechType.AdvancedWiringKit, 2)
                    
                })
            };
        }        
    }
}
