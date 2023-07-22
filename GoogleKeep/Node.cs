using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

/// <summary>
/// node.py
/// </summary>
namespace GoogleKeep
{
    /// <summary>
    /// Valid note types.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum NodeType
    {
        /// <summary>
        /// A Note
        /// </summary>
        [JsonPropertyName("NOTE")]
        Note,

        /// <summary>
        /// A List
        /// </summary>
        [JsonPropertyName("LIST")]
        List,

        /// <summary>
        /// A List item
        /// </summary>
        [JsonPropertyName("LIST_ITEM")]
        ListItem,

        /// <summary>
        /// A blob (attachment)
        /// </summary>
        [JsonPropertyName("BLOB")]
        Blob
    }

    /// <summary>
    /// Valid blob types.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum BlobType
    {
        /// <summary>
        /// Audio
        /// </summary>
        [JsonPropertyName("AUDIO")]
        Audio,

        /// <summary>
        /// Image
        /// </summary>
        [JsonPropertyName("IMAGE")]
        Image,

        /// <summary>
        /// Drawing
        /// </summary>
        [JsonPropertyName("DRAWING")]
        Drawing
    }

    /// <summary>
    /// Valid note colors.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ColorValue
    {
        /// <summary>
        /// White
        /// </summary>
        [JsonPropertyName("DEFAULT")]
        White,

        /// <summary>
        /// Red
        /// </summary>
        [JsonPropertyName("RED")]
        Red,

        /// <summary>
        /// Orange
        /// </summary>
        [JsonPropertyName("ORANGE")]
        Orange,

        /// <summary>
        /// Yellow
        /// </summary>
        [JsonPropertyName("YELLOW")]
        Yellow,

        /// <summary>
        /// Green
        /// </summary>
        [JsonPropertyName("GREEN")]
        Green,

        /// <summary>
        /// Teal
        /// </summary>
        [JsonPropertyName("TEAL")]
        Teal,

        /// <summary>
        /// Blue
        /// </summary>
        [JsonPropertyName("BLUE")]
        Blue,

        /// <summary>
        /// Dark blue
        /// </summary>
        [JsonPropertyName("CERULEAN")]
        DarkBlue,

        /// <summary>
        /// Purple
        /// </summary>
        [JsonPropertyName("PURPLE")]
        Purple,

        /// <summary>
        /// Pink
        /// </summary>
        [JsonPropertyName("PINK")]
        Pink,

        /// <summary>
        /// Brown
        /// </summary>
        [JsonPropertyName("BROWN")]
        Brown,

        /// <summary>
        /// Gray
        /// </summary>
        [JsonPropertyName("GRAY")]
        Gray
    }

    /// <summary>
    /// Valid note categories.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CategoryValue
    {
        /// <summary>
        /// Books
        /// </summary>
        [JsonPropertyName("BOOKS")]
        Books,

        /// <summary>
        /// Food
        /// </summary>
        [JsonPropertyName("FOOD")]
        Food,

        /// <summary>
        /// Movies
        /// </summary>
        [JsonPropertyName("MOVIES")]
        Movies,

        /// <summary>
        /// Music
        /// </summary>
        [JsonPropertyName("MUSIC")]
        Music,

        /// <summary>
        /// Places
        /// </summary>
        [JsonPropertyName("PLACES")]
        Places,

        /// <summary>
        /// Quotes
        /// </summary>
        [JsonPropertyName("QUOTES")]
        Quotes,

        /// <summary>
        /// Travel
        /// </summary>
        [JsonPropertyName("TRAVEL")]
        Travel,

        /// <summary>
        /// TV
        /// </summary>
        [JsonPropertyName("TV")]
        TV
    }

    /// <summary>
    /// Valid task suggestion categories.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SuggestValue
    {
        /// <summary>
        /// Grocery item
        /// </summary>
        [JsonPropertyName("GROCERY_ITEM")]
        GroceryItem
    }

    /// <summary>
    /// Target location to put new list items.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum NewListItemPlacementValue
    {
        /// <summary>
        /// Top
        /// </summary>
        [JsonPropertyName("TOP")]
        Top,

        /// <summary>
        /// Bottom
        /// </summary>
        [JsonPropertyName("BOTTOM")]
        Bottom
    }

    /// <summary>
    /// Visibility setting for the graveyard.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum GraveyardStateValue
    {
        /// <summary>
        /// Expanded
        /// </summary>
        [JsonPropertyName("EXPANDED")]
        Expanded,

        /// <summary>
        /// Collapsed
        /// </summary>
        [JsonPropertyName("COLLAPSED")]
        Collapsed
    }

    /// <summary>
    /// Movement setting for checked list items.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CheckedListItemsPolicyValue
    {
        /// <summary>
        /// Default
        /// </summary>
        [JsonPropertyName("DEFAULT")]
        Default,

        /// <summary>
        /// Graveyard
        /// </summary>
        [JsonPropertyName("GRAVEYARD")]
        Graveyard
    }

    /// <summary>
    /// Collaborator change type.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ShareRequestValue
    {
        /// <summary>
        /// Grant access.
        /// </summary>
        [JsonPropertyName("WR")]
        Add,

        /// <summary>
        /// Remove access.
        /// </summary>
        [JsonPropertyName("RM")]
        Remove
    }

    /// <summary>
    /// Collaborator role type.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RoleValue
    {
        /// <summary>
        /// Note owner.
        /// </summary>
        [JsonPropertyName("O")]
        Owner,

        /// <summary>
        /// Note collaborator.
        /// </summary>
        [JsonPropertyName("W")]
        User
    }
}
