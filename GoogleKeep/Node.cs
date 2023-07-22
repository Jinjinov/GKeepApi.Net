using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// node.py
/// </summary>
namespace GoogleKeep
{
    public enum NodeType
{
    /// <summary>
    /// A Note
    /// </summary>
    Note,
    
    /// <summary>
    /// A List
    /// </summary>
    List,
    
    /// <summary>
    /// A List item
    /// </summary>
    ListItem,
    
    /// <summary>
    /// A blob (attachment)
    /// </summary>
    Blob
}
}
