﻿using System.Collections.Generic;
using Orrery.HeartModule.Shared.Definitions;
using Orrery.HeartModule.Shared.Logging;
using Sandbox.Definitions;
using VRage.Game;
using VRage.Game.Components;

namespace Orrery.HeartModule.Client.Interface
{
    public static class BlockCategoryManager
    {
        private static GuiBlockCategoryHelper _orreryBlockCategory;

        public static void Init()
        {
            _orreryBlockCategory = new GuiBlockCategoryHelper("[Orrery Combat Framework]", "OrreryBlockCategory");
            HeartLog.Info("BlockCategoryManager initialized.");
        }

        public static void RegisterFromDefinition(WeaponDefinitionBase definition)
        {
            _orreryBlockCategory.AddBlock(definition.Assignments.BlockSubtype);
        }

        public static void Close()
        {
            _orreryBlockCategory = null;
            HeartLog.Info("BlockCategoryManager closed.");
        }

        private class GuiBlockCategoryHelper
        {
            private readonly MyGuiBlockCategoryDefinition _category;

            public GuiBlockCategoryHelper(string name, string id)
            {
                _category = new MyGuiBlockCategoryDefinition
                {
                    Id = new MyDefinitionId(typeof(MyObjectBuilder_GuiBlockCategoryDefinition), id),
                    Name = name,
                    DisplayNameString = name,
                    ItemIds = new HashSet<string>(),
                    IsBlockCategory = true,
                };
                MyDefinitionManager.Static.GetCategories().Add(name, _category);
            }

            public void AddBlock(string subtypeId)
            {
                if (!_category.ItemIds.Contains(subtypeId))
                    _category.ItemIds.Add(subtypeId);

                //foreach (var _cat in MyDefinitionManager.Static.GetCategories().Values)
                //{
                //    HeartData.I.Log.Log("Category " + _cat.Name);
                //    foreach (var _id in _cat.ItemIds)
                //        HeartData.I.Log.Log($"   \"{_id}\"");
                //}
            }
        }
    }
}