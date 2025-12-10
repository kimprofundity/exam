using HRPayrollSystem.API.Data;
using HRPayrollSystem.API.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace HRPayrollSystem.API.Services;

/// <summary>
/// 費率表服務實作
/// </summary>
public class RateTableService : IRateTableService
{
    private readonly HRPayrollContext _context;
    private readonly ILogger<RateTableService> _logger;

    public RateTableService(
        HRPayrollContext context,
        ILogger<RateTableService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 建立費率表
    /// </summary>
    public async Task<RateTable> CreateAsync(RateTable rateTable, string createdBy)
    {
        try
        {
            // 驗證費率格式
            ValidateRateTable(rateTable);

            // 檢查是否有重疊的生效期間
            var overlapping = await _context.RateTables
                .Where(r => r.EffectiveDate <= (rateTable.ExpiryDate ?? DateTime.MaxValue) &&
                           (r.ExpiryDate == null || r.ExpiryDate >= rateTable.EffectiveDate))
                .FirstOrDefaultAsync();

            if (overlapping != null)
            {
                throw new InvalidOperationException(
                    $"費率表生效期間與現有費率表重疊（版本：{overlapping.Version}）");
            }

            // 設定基本資訊
            rateTable.Id = Guid.NewGuid().ToString();
            rateTable.CreatedBy = createdBy;
            rateTable.CreatedAt = DateTime.UtcNow;

            _context.RateTables.Add(rateTable);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "建立費率表：版本 {Version}，生效日期：{EffectiveDate}，來源：{Source}",
                rateTable.Version, rateTable.EffectiveDate, rateTable.Source);

            return rateTable;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "建立費率表時發生錯誤");
            throw;
        }
    }

    /// <summary>
    /// 更新費率表
    /// </summary>
    public async Task<RateTable> UpdateAsync(string id, RateTable rateTable)
    {
        try
        {
            var existing = await _context.RateTables.FindAsync(id);
            if (existing == null)
            {
                throw new KeyNotFoundException($"找不到識別碼為 {id} 的費率表");
            }

            // 驗證費率格式
            ValidateRateTable(rateTable);

            // 更新欄位
            existing.Version = rateTable.Version;
            existing.EffectiveDate = rateTable.EffectiveDate;
            existing.ExpiryDate = rateTable.ExpiryDate;
            existing.LaborInsuranceRate = rateTable.LaborInsuranceRate;
            existing.HealthInsuranceRate = rateTable.HealthInsuranceRate;
            existing.Source = rateTable.Source;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "更新費率表：版本 {Version}",
                existing.Version);

            return existing;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新費率表時發生錯誤");
            throw;
        }
    }

    /// <summary>
    /// 取得費率表
    /// </summary>
    public async Task<RateTable?> GetByIdAsync(string id)
    {
        return await _context.RateTables.FindAsync(id);
    }

    /// <summary>
    /// 取得生效的費率表（依日期）
    /// </summary>
    public async Task<RateTable?> GetEffectiveRateTableAsync(DateTime date)
    {
        return await _context.RateTables
            .Where(r => r.EffectiveDate <= date &&
                       (r.ExpiryDate == null || r.ExpiryDate >= date))
            .OrderByDescending(r => r.EffectiveDate)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// 取得所有費率表
    /// </summary>
    public async Task<List<RateTable>> GetAllRateTablesAsync()
    {
        return await _context.RateTables
            .OrderByDescending(r => r.EffectiveDate)
            .ToListAsync();
    }

    /// <summary>
    /// 取得費率表歷史
    /// </summary>
    public async Task<List<RateTable>> GetRateTableHistoryAsync()
    {
        return await _context.RateTables
            .OrderByDescending(r => r.EffectiveDate)
            .ToListAsync();
    }

    /// <summary>
    /// 匯入費率表（從檔案）
    /// </summary>
    public async Task<RateTable> ImportFromFileAsync(Stream fileStream, string fileName, string createdBy)
    {
        try
        {
            string content;
            using (var reader = new StreamReader(fileStream, Encoding.UTF8))
            {
                content = await reader.ReadToEndAsync();
            }

            RateTable? rateTable = null;

            // 根據檔案類型解析
            if (fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                rateTable = JsonSerializer.Deserialize<RateTable>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            else if (fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                rateTable = ParseCsvFile(content);
            }
            else
            {
                throw new ArgumentException("不支援的檔案格式，僅支援 JSON 和 CSV");
            }

            if (rateTable == null)
            {
                throw new InvalidOperationException("無法解析檔案內容");
            }

            // 設定來源為檔案
            rateTable.Source = "File";

            // 建立費率表
            return await CreateAsync(rateTable, createdBy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "匯入費率表時發生錯誤");
            throw;
        }
    }

    /// <summary>
    /// 刪除費率表
    /// </summary>
    public async Task<bool> DeleteAsync(string id)
    {
        try
        {
            var rateTable = await _context.RateTables.FindAsync(id);
            if (rateTable == null)
            {
                return false;
            }

            _context.RateTables.Remove(rateTable);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "刪除費率表：版本 {Version}",
                rateTable.Version);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刪除費率表時發生錯誤");
            throw;
        }
    }

    /// <summary>
    /// 驗證費率表
    /// </summary>
    private void ValidateRateTable(RateTable rateTable)
    {
        if (string.IsNullOrWhiteSpace(rateTable.Version))
        {
            throw new ArgumentException("費率表版本不能為空");
        }

        if (rateTable.LaborInsuranceRate < 0 || rateTable.LaborInsuranceRate > 1)
        {
            throw new ArgumentException("勞保費率必須在 0 到 1 之間");
        }

        if (rateTable.HealthInsuranceRate < 0 || rateTable.HealthInsuranceRate > 1)
        {
            throw new ArgumentException("健保費率必須在 0 到 1 之間");
        }

        if (rateTable.ExpiryDate.HasValue && rateTable.ExpiryDate < rateTable.EffectiveDate)
        {
            throw new ArgumentException("失效日期不能早於生效日期");
        }
    }

    /// <summary>
    /// 解析 CSV 檔案
    /// </summary>
    private RateTable ParseCsvFile(string content)
    {
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        
        if (lines.Length < 2)
        {
            throw new InvalidOperationException("CSV 檔案格式不正確");
        }

        // 假設第一行是標題，第二行是資料
        // 格式：Version,EffectiveDate,ExpiryDate,LaborInsuranceRate,HealthInsuranceRate
        var dataLine = lines[1].Split(',');

        if (dataLine.Length < 5)
        {
            throw new InvalidOperationException("CSV 檔案資料不完整");
        }

        return new RateTable
        {
            Version = dataLine[0].Trim(),
            EffectiveDate = DateTime.Parse(dataLine[1].Trim()),
            ExpiryDate = string.IsNullOrWhiteSpace(dataLine[2]) ? null : DateTime.Parse(dataLine[2].Trim()),
            LaborInsuranceRate = decimal.Parse(dataLine[3].Trim()),
            HealthInsuranceRate = decimal.Parse(dataLine[4].Trim())
        };
    }
}
