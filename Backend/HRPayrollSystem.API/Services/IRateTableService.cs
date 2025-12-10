using HRPayrollSystem.API.Models;

namespace HRPayrollSystem.API.Services;

/// <summary>
/// 費率表服務介面
/// </summary>
public interface IRateTableService
{
    /// <summary>
    /// 建立費率表
    /// </summary>
    Task<RateTable> CreateAsync(RateTable rateTable, string createdBy);

    /// <summary>
    /// 更新費率表
    /// </summary>
    Task<RateTable> UpdateAsync(string id, RateTable rateTable);

    /// <summary>
    /// 取得費率表
    /// </summary>
    Task<RateTable?> GetByIdAsync(string id);

    /// <summary>
    /// 取得生效的費率表（依日期）
    /// </summary>
    Task<RateTable?> GetEffectiveRateTableAsync(DateTime date);

    /// <summary>
    /// 取得所有費率表
    /// </summary>
    Task<List<RateTable>> GetAllRateTablesAsync();

    /// <summary>
    /// 取得費率表歷史
    /// </summary>
    Task<List<RateTable>> GetRateTableHistoryAsync();

    /// <summary>
    /// 匯入費率表（從檔案）
    /// </summary>
    Task<RateTable> ImportFromFileAsync(Stream fileStream, string fileName, string createdBy);

    /// <summary>
    /// 刪除費率表
    /// </summary>
    Task<bool> DeleteAsync(string id);
}
