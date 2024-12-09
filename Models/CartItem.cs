// Imports

namespace SampleCode
{
    /**
     * Cart Item Model Class
    **/
    public class CartItem : EntityBase
    {
        public int HeaderId { get; set; } = -1;
        public int DetailId { get; set; } = -1;
        public long PropertyId { get; set; } = -1;
        public string PropertyName { get; set; }
        public long LocationId { get; set; } = -1;
        public string LocationName { get; set; }
        public int? FamilyGroupId { get; set; } = -1;
        public string? FamilyGroupName { get; set; }
        public int CatalogId { get; set; } = -1;
        public string CatalogName { get; set; }
        public int? ProductId { get; set; } = -1;
        public int? ObjectNumber { get; set; } = -1;
        public int OwnerUserId { get; set; }
        public string OwnerAnetUserId { get; set; }
        public int? SubmittedUserId { get; set; }
        public string? SubmittedByAnetUserId { get; set; }
        public int HeaderStatusId { get; set; } = -1;
        public string HeaderStatusName { get; set; }
        public int DetailStatusId { get; set; } = -1;
        public string DetailStatusName { get; set; }
        public string FirstName { get; set; }
        public string? SecondName { get; set; }
        public int? SLUId { get; set; }
        public int? OriginalSLUId { get; set; }
        public string SLU { get; set; }
        public string? OriginalSLU { get; set; }
        public string? Barcode { get; set; }
        public decimal? Price { get; set; }
        public decimal? Cost { get; set; }
        public decimal? OriginalPrice { get; set; }
        public decimal? OriginalCost { get; set; }
        public bool OnMenu { get; set; }
        public bool OriginalOnMenu { get; set; }
        public DateTime HeaderCreateDate { get; set; }
        public DateTime HeaderModifiedDate { get; set; }
        public DateTime DetailCreateDate { get; set; }
        public DateTime DetailModifiedDate { get; set; }
        public bool IsProcessing { get; set; }
        public bool IsSubmissionFailed { get; set; } = false;
        public bool IsCPG { get; set; }
        public long? MenuItemDefId { get; set; }
        public long? MenuItemPriceID { get; set; }
        public string? Description { get; set; }
        public int? FamilyGroupObjectNumber { get; set; }
        public int? MajorGroupObjectNumber { get; set; }
        public long? CNCPropertyId { get; set; }
        public long? CNCLocationId { get; set; }
        public int? MenuItemClass { get; set; }
        public int? TaxClass { get; set; }
        public string? TaxClassName { get; set; }
        public int? OriginalTaxClass { get; set; }
        public long? ScheduleTimeStamp { get; set; }
        // SLU 2 
        public int? SLU2Id { get; set; }
        public int? OriginalSLU2Id { get; set; }
        public string? SLU2 { get; set; }
        public string? OriginalSLU2 { get; set; }

        public string? ConsumerItemName { get; set; }
        public string? ConsumerItemDescription { get; set; }
        public string? OriginalConsumerItemName { get; set; }
        public string? OriginalConsumerItemDescription { get; set; }
        public int? PrintClassOverride { get; set; }
        public int? OriginalPrintClassOverride { get; set; }
        public int? OriginalMenuItemClass { get; set; }
        public string? OriginalMenuItemClassName { get; set; }
        public string? MenuItemClassName { get; set; }
        public string? DefaultTaxClassName { get; set; }
        public string? RecipeId { get; set; }
        [NotMapped]
        public List<BaseTag>? MashginTagJson { get; set; }
        public string? MashginTag
        {
            get => JsonConvert.SerializeObject(MashginTagJson);
            set => MashginTagJson = string.IsNullOrEmpty(value) ? null : JsonConvert.DeserializeObject<List<BaseTag>>(value);
        }
        [NotMapped]
        public List<BaseTag>? OriginalMashginTagJson { get; set; }
        public string? OriginalMashginTag
        {
            get => JsonConvert.SerializeObject(OriginalMashginTagJson);
            set => OriginalMashginTagJson = string.IsNullOrEmpty(value) ? null : JsonConvert.DeserializeObject<List<BaseTag>>(value);
        }
    }
}