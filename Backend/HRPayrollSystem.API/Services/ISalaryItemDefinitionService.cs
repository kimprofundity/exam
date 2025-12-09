using HRPayrollSystem.API.Models;

namespace HRPayrollSystem.API.Services;

/// <summary>
/// 薪資項目定義服務介面
/// </summary>
public interface ISalaryItemDefinitionService
{
    /// <summary>
    /// 建立薪資項目定義
    /// </summary>
    Task<SalaryItemDefinition> CreateAsync(SalaryItemDefinition definition, string createdBy);

    /// <summary>
    /// 更新薪資項目定義
    /// </summary>
    Task<SalaryItemDefinition> UpdateAsync(string id, SalaryItemDefinition definition);

    /// <summary>
    /// 取得薪資項目定義
    /// </summary>
    Task<SalaryItemDefinition?> GetByIdAsync(string id);

    /// <summary>
    /// 取得薪資項目定義（依項目代碼和生效日期）
    /// </summary>
    Task<SalaryItemDefinition?> GetByCodeAndDateAsync(string itemCode, DateTime effectiveDate);

    /// <summary>
    /// 取得所有啟用的薪資項目定義
    /// </summary>
    Task<List<SalaryItemDefinition>> GetActiveItemsAsync();

    /// <summary>
    /// 取得所有薪資項目定義（含停用）
    /// </summary>
    Task<List<SalaryItemDefinition>> GetAllItemsAsync();

    /// <summary>
    /// 取得特定類型的薪資項目定義
    /// </summary>
    Task<List<SalaryItemDefinition>> GetItemsByTypeAsync(SalaryItemType type);

    /// <summary>
    /// 停用薪資項目定義
    /// </summary>
    Task<bool> DeactivateAsync(string id);

    /// <summary>
    /// 取得薪資項目定義的歷史版本
    /// </summary>
    Task<List<SalaryItemDefinition>> GetItemHistoryAsync(string itemCode);
}
