using HRPayrollSystem.API.Data;
using HRPayrollSystem.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HRPayrollSystem.API.Services;

/// <summary>
/// 薪資項目定義服務實作
/// </summary>
public class SalaryItemDefinitionService : ISalaryItemDefinitionService
{
    private readonly HRPayrollContext _context;
    private readonly ILogger<SalaryItemDefinitionService> _logger;

    public SalaryItemDefinitionService(
        HRPayrollContext context,
        ILogger<SalaryItemDefinitionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 建立薪資項目定義
    /// </summary>
    public async Task<SalaryItemDefinition> CreateAsync(SalaryItemDefinition definition, string createdBy)
    {
        try
        {
            // 驗證項目代碼唯一性（同一生效日期）
            var existing = await _context.SalaryItemDefinitions
                .FirstOrDefaultAsync(x => 
                    x.ItemCode == definition.ItemCode && 
                    x.EffectiveDate == definition.EffectiveDate);

            if (existing != null)
            {
                throw new InvalidOperationException(
                    $"薪資項目代碼 {definition.ItemCode} 在生效日期 {definition.EffectiveDate:yyyy-MM-dd} 已存在");
            }

            // 驗證計算方式與對應欄位
            ValidateCalculationMethod(definition);

            // 設定基本資訊
            definition.Id = Guid.NewGuid().ToString();
            definition.CreatedBy = createdBy;
            definition.CreatedAt = DateTime.UtcNow;
            definition.UpdatedAt = DateTime.UtcNow;

            _context.SalaryItemDefinitions.Add(definition);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "建立薪資項目定義：{ItemCode} - {ItemName}，類型：{Type}，計算方式：{Method}",
                definition.ItemCode, definition.ItemName, definition.Type, definition.CalculationMethod);

            return definition;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "建立薪資項目定義時發生錯誤");
            throw;
        }
    }

    /// <summary>
    /// 更新薪資項目定義
    /// </summary>
    public async Task<SalaryItemDefinition> UpdateAsync(string id, SalaryItemDefinition definition)
    {
        try
        {
            var existing = await _context.SalaryItemDefinitions.FindAsync(id);
            if (existing == null)
            {
                throw new KeyNotFoundException($"找不到識別碼為 {id} 的薪資項目定義");
            }

            // 驗證計算方式與對應欄位
            ValidateCalculationMethod(definition);

            // 更新欄位
            existing.ItemName = definition.ItemName;
            existing.Type = definition.Type;
            existing.CalculationMethod = definition.CalculationMethod;
            existing.DefaultAmount = definition.DefaultAmount;
            existing.HourlyRate = definition.HourlyRate;
            existing.PercentageRate = definition.PercentageRate;
            existing.Description = definition.Description;
            existing.IsActive = definition.IsActive;
            existing.EffectiveDate = definition.EffectiveDate;
            existing.ExpiryDate = definition.ExpiryDate;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "更新薪資項目定義：{ItemCode} - {ItemName}",
                existing.ItemCode, existing.ItemName);

            return existing;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新薪資項目定義時發生錯誤");
            throw;
        }
    }

    /// <summary>
    /// 取得薪資項目定義
    /// </summary>
    public async Task<SalaryItemDefinition?> GetByIdAsync(string id)
    {
        return await _context.SalaryItemDefinitions.FindAsync(id);
    }

    /// <summary>
    /// 取得薪資項目定義（依項目代碼和生效日期）
    /// </summary>
    public async Task<SalaryItemDefinition?> GetByCodeAndDateAsync(string itemCode, DateTime effectiveDate)
    {
        return await _context.SalaryItemDefinitions
            .Where(x => x.ItemCode == itemCode && 
                       x.EffectiveDate <= effectiveDate &&
                       (x.ExpiryDate == null || x.ExpiryDate >= effectiveDate) &&
                       x.IsActive)
            .OrderByDescending(x => x.EffectiveDate)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// 取得所有啟用的薪資項目定義
    /// </summary>
    public async Task<List<SalaryItemDefinition>> GetActiveItemsAsync()
    {
        var today = DateTime.Today;
        return await _context.SalaryItemDefinitions
            .Where(x => x.IsActive && 
                       x.EffectiveDate <= today &&
                       (x.ExpiryDate == null || x.ExpiryDate >= today))
            .OrderBy(x => x.ItemCode)
            .ToListAsync();
    }

    /// <summary>
    /// 取得所有薪資項目定義（含停用）
    /// </summary>
    public async Task<List<SalaryItemDefinition>> GetAllItemsAsync()
    {
        return await _context.SalaryItemDefinitions
            .OrderBy(x => x.ItemCode)
            .ThenByDescending(x => x.EffectiveDate)
            .ToListAsync();
    }

    /// <summary>
    /// 取得特定類型的薪資項目定義
    /// </summary>
    public async Task<List<SalaryItemDefinition>> GetItemsByTypeAsync(SalaryItemType type)
    {
        var today = DateTime.Today;
        return await _context.SalaryItemDefinitions
            .Where(x => x.Type == type && 
                       x.IsActive && 
                       x.EffectiveDate <= today &&
                       (x.ExpiryDate == null || x.ExpiryDate >= today))
            .OrderBy(x => x.ItemCode)
            .ToListAsync();
    }

    /// <summary>
    /// 停用薪資項目定義
    /// </summary>
    public async Task<bool> DeactivateAsync(string id)
    {
        try
        {
            var definition = await _context.SalaryItemDefinitions.FindAsync(id);
            if (definition == null)
            {
                return false;
            }

            definition.IsActive = false;
            definition.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "停用薪資項目定義：{ItemCode} - {ItemName}",
                definition.ItemCode, definition.ItemName);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停用薪資項目定義時發生錯誤");
            throw;
        }
    }

    /// <summary>
    /// 取得薪資項目定義的歷史版本
    /// </summary>
    public async Task<List<SalaryItemDefinition>> GetItemHistoryAsync(string itemCode)
    {
        return await _context.SalaryItemDefinitions
            .Where(x => x.ItemCode == itemCode)
            .OrderByDescending(x => x.EffectiveDate)
            .ToListAsync();
    }

    /// <summary>
    /// 驗證計算方式與對應欄位
    /// </summary>
    private void ValidateCalculationMethod(SalaryItemDefinition definition)
    {
        switch (definition.CalculationMethod)
        {
            case CalculationMethod.Fixed:
                if (!definition.DefaultAmount.HasValue || definition.DefaultAmount.Value <= 0)
                {
                    throw new ArgumentException("固定金額計算方式必須設定預設金額");
                }
                break;

            case CalculationMethod.Hourly:
                if (!definition.HourlyRate.HasValue || definition.HourlyRate.Value <= 0)
                {
                    throw new ArgumentException("按小時計算方式必須設定小時費率");
                }
                break;

            case CalculationMethod.Percentage:
                if (!definition.PercentageRate.HasValue || 
                    definition.PercentageRate.Value <= 0 || 
                    definition.PercentageRate.Value > 1)
                {
                    throw new ArgumentException("按比例計算方式必須設定比例費率（0-1 之間）");
                }
                break;
        }
    }
}
