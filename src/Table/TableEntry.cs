using System;
using FistVR;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using Valve.Newtonsoft.Json;

namespace Lootations
{
    public class TableEntry
    {
        public enum TableType
        {
            OBJECT_ID = 0,
            TABLE_REFERENCE = 1,
            TAGS = 2,
        };

        public TableType Type { get; set; }
        public int Weight { get; set; }
        public string Meta { get; set; }
        public string[] LootIds { get; set; }

        public static TableEntry ObjectEntry(string objectId, int weight)
        {
            return new TableEntry { Weight = weight, LootIds = [ objectId ], Type = TableType.OBJECT_ID };
        }

        public static TableEntry TableReference(string tableName, int weight)
        {
            return new TableEntry { Weight = weight, LootIds = [ tableName ] , Type = TableType.TABLE_REFERENCE };
        }

        public string[] RollObjectId()
        {
            switch (Type)
            {
                case TableType.OBJECT_ID:
                    return LootIds;
                case TableType.TABLE_REFERENCE:
                    return TableManager.GetTable(LootIds[0]).RollObjectId();
                case TableType.TAGS:
                    // TODO:
                    return [];
            }
            Lootations.Logger.LogError("Invalid TableEntry Type. Returning None.");
            return [];
        }

        private bool IsTagAndValueValid(string tag, string value)
        {
            // BulbBlue is used just to get an object.
            Type fvrObjectType = IM.OD["BulbBlue"].GetType();
            PropertyInfo info = fvrObjectType.GetProperty(tag.Insert(0, "Tag"));
            if (info is null)
            {
                Lootations.Logger.LogError("Could not find tag with name " + info);
                return false;
            }

            // TODO: Check if the enum "OTag" has the value defined
            return true;
        }

        private void InitializeTagTable()
        {
            if (Type != TableType.TAGS || LootIds.Length == 0)
                return;

            string[] splitTag = LootIds[0].Split(':');
            if (splitTag.Length != 2)
            {
                Lootations.Logger.LogError("Error parsing tags in tag table entry, invalid argument length");
                return;
            }
            string tag = splitTag[0];
            string value = splitTag[1];
            List<string> newTable = new List<string>();
            if (!IsTagAndValueValid(tag, value))
            {

            }
            foreach (var item in IM.OD)
            {

            }
        }
    }
}
