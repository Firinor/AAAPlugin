#if UNITY_2020_1_OR_NEWER
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;

public class NestedSearchDropdown : AdvancedDropdown
{
    private string _selectedPath;
    private bool _isNeedGroup;
    private Action<string> _onItemSelected;
    private IOrderedEnumerable<string> _items;
    private Dictionary<int, string> _idToPathMap = new Dictionary<int, string>();

    public NestedSearchDropdown(AdvancedDropdownState state, string[] content, Action<string> onItemSelected, bool isNeedGroup = true) : base(state)
    {
        _onItemSelected = onItemSelected;
        _isNeedGroup = isNeedGroup;
        _items = from item in content orderby item select item;//Alphabetically sorted
    }

    protected override AdvancedDropdownItem BuildRoot()
    {
        _idToPathMap.Clear();
        var root = new AdvancedDropdownItem("Select Item");

        List<string> groups = new();

        int id = 0;
        if (_isNeedGroup)
        {
            foreach (var item in _items)
            {
                AddItemWithPath(root, "", item, ref id);
            }
        }
        else
        {
            foreach (var item in _items)
            {
                var newDropdownItem = new AdvancedDropdownItem(item) { id = id };
                root.AddChild(newDropdownItem);
                _idToPathMap.Add(id, item);
                id++;
            }
        }
        return root;
    }

    private void AddItemWithPath(AdvancedDropdownItem parent, string parentName, string name, ref int id)
    {
        string[] splitName = name.Split(".", 2);

        if (splitName.Length > 1)
        {
            var groupName = splitName[0];

            AdvancedDropdownItem group = null;
            foreach (var parentItem in parent.children)
            {
                if (parentItem.name == groupName)
                {
                    group = parentItem;
                    break;
                }
            }
            if(group is null)
            {
                group = new AdvancedDropdownItem(groupName);
                parent.AddChild(group);
            }
            
            AddItemWithPath(group, (String.IsNullOrEmpty(parentName) ? groupName : parentName + "." + groupName), splitName[1], ref id);
            return;
        }
        
        var newDropdownItem = new AdvancedDropdownItem(FullName()) { id = id };
        parent.AddChild(newDropdownItem);
        _idToPathMap.Add(id, FullName());
        id++;

        string FullName()
        {
            return (String.IsNullOrEmpty(parentName) ? name : parentName + "." + name);
        }
    }

    protected override void ItemSelected(AdvancedDropdownItem item)
    {
        base.ItemSelected(item);
        
        if (_idToPathMap.TryGetValue(item.id, out var path))
        {
            _selectedPath = path;
            _onItemSelected?.Invoke(_selectedPath);
        }
    }
}
#endif