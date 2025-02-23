using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

class LumiRailLite
{
    static void Main()
    {
        // 输出程序版本号
        const string version = "1.0 Lite";
        Console.WriteLine($"LumiRail 版本号: {version}");
        
        // 获取当前程序运行的目录，即该程序所在的文件夹路径
        string currentDirectory = Directory.GetCurrentDirectory();
        
        // 获取当前目录下所有扩展名为 .jpg 的图片文件
        var jpgFiles = Directory.GetFiles(currentDirectory, "*.jpg");
        int totalFiles = jpgFiles.Length;
        int processedFiles = 0;

        // 创建日志文件
        string logFilePath = Path.Combine(currentDirectory, "processing_log.csv");
        using (var logFile = new StreamWriter(logFilePath))
        {
            // 写入日志文件的表头
            logFile.WriteLine("Status,FileName,DateTaken,NewFilePath,ErrorMessage");

            // 遍历所有找到的 JPG 图片
            foreach (var file in jpgFiles)
            {
                string status = "Success";
                string errorMessage = string.Empty;
                string newFilePath = string.Empty;
                string dateTaken = string.Empty;

                try
                {
                    // 使用 Image.FromFile() 方法打开图片文件，以便读取其 Exif 元数据
                    using (Image image = Image.FromFile(file))
                    {
                        // 调用 GetExifDate() 方法，尝试获取照片的拍摄日期信息
                        dateTaken = GetExifDate(image);
                        
                        if (!string.IsNullOrEmpty(dateTaken)) // 确保成功获取到了拍摄日期信息
                        {
                            // 提取日期部分（YYYYMMDD），用于创建归档文件夹
                            string folderName = dateTaken.Substring(0, 8);
                            
                            // 获取原始文件名（包含扩展名）
                            string originalFileName = Path.GetFileName(file);
                            
                            // 生成新的文件名，格式为：拍摄日期_原文件名（保留原文件名以便识别）
                            string newFileName = dateTaken + "_" + originalFileName;
                            
                            // 目标文件夹路径，按拍摄日期创建归档目录
                            string newFolderPath = Path.Combine(currentDirectory, folderName);
                            
                            // 如果目标文件夹不存在，则创建该文件夹
                            if (!Directory.Exists(newFolderPath))
                            {
                                Directory.CreateDirectory(newFolderPath);
                            }
                            
                            // 生成目标文件的新完整路径
                            newFilePath = Path.Combine(newFolderPath, newFileName);

                            image.Dispose();
                            
                            // 检查目标文件是否已存在，避免文件覆盖
                            if (!File.Exists(newFilePath))
                            {
                                // 将图片移动到目标文件夹，并重命名
                                File.Move(file, newFilePath);
                            }
                        }
                        else
                        {
                            // 如果无法获取拍摄日期，则记录失败状态和错误信息
                            status = "Failed";
                            errorMessage = "无法获取拍摄日期";
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 捕获并记录异常信息，防止程序因错误终止
                    status = "Failed";
                    errorMessage = ex.Message;
                }

                // 写入日志文件
                logFile.WriteLine($"{status},{file},{dateTaken},{newFilePath},{errorMessage}");

                // 更新进度条
                processedFiles++;
                DrawProgressBar(processedFiles, totalFiles);
            }
        }
    }

    // 获取 Exif 拍摄日期的方法
    static string GetExifDate(Image image)
    {
        // Exif 中常见的日期字段 ID（不同相机可能使用不同的 Exif 标签）
        int[] dateTags = { 0x9003, 0x0132, 0x9004 }; // 分别代表拍摄时间、修改时间等
        
        foreach (var tag in dateTags) // 遍历所有可能存储拍摄日期的 Exif 标签
        {
            // 尝试获取指定 ID 的 Exif 信息
            PropertyItem propItem = image.PropertyItems.FirstOrDefault(p => p.Id == tag);
            if (propItem != null)
            {
                // Exif 格式的日期时间通常为 "YYYY:MM:DD HH:MM:SS"
                string dateTaken = System.Text.Encoding.ASCII.GetString(propItem.Value);
                
                // 使用正则表达式提取日期和时间部分，并格式化为 "YYYYMMDD_HHMMSS"
                dateTaken = Regex.Replace(dateTaken, @"[^\d]", "");
                if (dateTaken.Length >= 14)
                {
                    dateTaken = dateTaken.Substring(0, 8) + "_" + dateTaken.Substring(8, 6);
                }
                
                return dateTaken;
            }
        }
        // 如果未找到任何有效的 Exif 日期信息，则返回 null
        return null;
    }

    // 绘制进度条的方法
    static void DrawProgressBar(int progress, int total)
    {
        int barWidth = 50; // 进度条的总宽度
        int progressWidth = (int)((double)progress / total * barWidth); // 根据进度计算进度条的填充宽度
        string progressBar = new string('#', progressWidth) + new string('-', barWidth - progressWidth); // 生成进度条字符串
        Console.Write($"\r[{progressBar}] {progress}/{total}"); // 在控制台中输出进度条
    }
}
