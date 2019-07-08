using System;
using System.IO;
using System.Threading.Tasks;
using MicroBatchFramework;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace UnzipAndOrganizeDatasetForUnity
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await BatchHost.CreateDefaultBuilder().RunBatchEngineAsync<MainBatch>(args);
        }
    }

    public class MainBatch : BatchBase
    {
        private bool CreateDirectory(string checkPath)
        {
            if (!Directory.Exists(checkPath))
            {
                Directory.CreateDirectory(checkPath);
                return true;
            }

            return false;
        }

        private void DeleteDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path);
            }
        }

        private void ZipExtract(string[] zipFilePaths, string temporaryWorkSpacePath, string outputPath)
        {
            Parallel.ForEach(zipFilePaths, zipFilePath =>
            {
                //解凍して
                var outputZipPath = temporaryWorkSpacePath + Path.GetFileName(zipFilePath);
                ZipFile.ExtractToDirectory(zipFilePath, outputZipPath);

                //objが入っているか見る。
                var objFiles = Directory.GetFiles(zipFilePath, "*.obj");

                //ディレクトリの中にzipファイルがある場合、解凍して中身を確かめないといけない。
                var zipFiles = Directory.GetFiles(zipFilePath, "*.zip");
                if (zipFiles != null && zipFiles.Length >= 1)
                {
                    ZipExtract(zipFiles, temporaryWorkSpacePath, outputPath);
                }

                //存在していればoutputPathに吐き出す。
                if (objFiles != null && objFiles.Length >= 1)
                {
                    var dirName = Path.GetFileName(zipFilePath).SnakeToUpperCamel();
                    var index = dirName.IndexOf("Model", StringComparison.CurrentCultureIgnoreCase);

                    //Model 〜〜みたいな感じで始まるやつはModelを削除する。
                    if (index == 0)
                    {
                        dirName = dirName.Substring(5);
                    }

                    var parentDirName = Regex.Replace(dirName, @"[0-9]", "");
                    CreateDirectory(Path.Join(outputPath, parentDirName));

                    Directory.Move(outputZipPath, Path.Join(outputPath, parentDirName, dirName));
                    Console.WriteLine($"{outputZipPath} done");
                }
            });
        }

        /// <summary>
        /// もとのフォルダには影響を与えないように。
        /// 
        /// フォルダを全探索してzipだったら解凍。
        /// ファイル名はすべてアッパーキャメルに一度直してから処理を行う。
        /// zipの中身が.objを含んでいたらoutputPathにそのzip名から
        /// 数字と拡張子と"Model"を抜いたものを名前として持つディレクトリに。
        /// 数字を削除していないやつを保存する。
        /// 例)
        /// Model_Noodle_1.0.zipにたいなのがあったら、
        /// outputPath/Noodle/Noodle1.0/model.objになる感じ。
        /// </summary>
        /// <param name="targetPath"></param>
        /// <param name="outputPath"></param>
        /// <param name="temporaryWorkSpacePath"></param>
        /// <returns></returns>
        public void UnzipAndOrganize([Option(0)]string targetPath, [Option(1)]string outputPath,
            [Option("tmp", "Temporary work space. It will be deleted at the end of the program.")]
            string temporaryWorkSpacePath = "tempWorkSpace")
        {
            try
            {
                temporaryWorkSpacePath += $"{DateTime.Now:yyyyMMddHHmmss}";

                if (!CreateDirectory(temporaryWorkSpacePath) || !CreateDirectory(outputPath))
                {
                    DeleteDirectory(temporaryWorkSpacePath);
                    DeleteDirectory(outputPath);
                    return;
                }

                //zipファイル全部集めてくる。
                string[] zipFilePaths = Directory.GetFiles(targetPath, "*.zip", SearchOption.AllDirectories);

                ZipExtract(zipFilePaths, temporaryWorkSpacePath, outputPath);
            }
            catch (Exception e)
            {
                DeleteDirectory(outputPath);
                Console.WriteLine(e);
            }
            finally
            {
                DeleteDirectory(temporaryWorkSpacePath);
            }
        }
    }
}