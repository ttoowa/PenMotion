using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PenMotion.System;

namespace PenMotion.Datas.Items {
    public class MotionItemBase {
        public MotionFile OwnerFile { get; private set; }
        public MotionFolderItem Parent { get; set; }

        public bool IsRoot => Parent == null;

        public string Guid { get; set; }
        public MotionItemType Type { get; protected set; }
        public string Name { get; private set; }

        public delegate void NameChangedDelegate(string oldName, string newName);

        public event NameChangedDelegate NameChanged;

        public MotionItemBase(MotionFile ownerFile, MotionItemType type) {
            Guid = global::System.Guid.NewGuid().ToString();
            OwnerFile = ownerFile;
            Type = type;
        }

        public bool SetName(string newName) {
            string oldName = Name;

            Name = newName;

            NameChanged?.Invoke(oldName, newName);
            return true;
        }
    }
}