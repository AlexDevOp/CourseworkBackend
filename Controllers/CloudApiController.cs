using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations;
using СourseworkBackend.CustomAttributes;
using СourseworkBackend.Models;
using static System.Net.WebRequestMethods;

namespace СourseworkBackend.Controllers
{
    [Route("api/cloud/")]
    [Produces("application/json")]
    [ApiController]
    public partial class CloudApiController : ControllerBase
    {
        [Route("load_structure")]
        [HttpPost]
        [ValidSessionRequired]
        public IActionResult LoadCloudStructure()
        {
            Session? session = Request.RouteValues["Session"] as Session;

            if (session == null)
            {
                Response.StatusCode = 403;
                return new UnauthorizedResult();
            }

            UserFileSystemStructure? userFileSystemStructure = GlobalScope.database.UsersFileSystemStructures.Where(structure => structure.User == session.User).FirstOrDefault();

            if (userFileSystemStructure == null)
            {
                FileSystemStructureFolder rootFolder = new FileSystemStructureFolder();
                var serializedStructure = JObject.FromObject(rootFolder).ToString();
                userFileSystemStructure = new UserFileSystemStructure { SerializedStructure = serializedStructure, User = session.User, Timestamp = DateTime.Now };
                GlobalScope.database.UsersFileSystemStructures.Add(userFileSystemStructure);
                GlobalScope.database.SaveChangesAsync().Wait();
                return new ObjectResult( new LoadCloudStructure { Structure = rootFolder } );
            }
            else
            {
                return new ObjectResult(new LoadCloudStructure { Structure = JObject.Parse(userFileSystemStructure.SerializedStructure).ToObject<FileSystemStructureFolder>(), LastEdit = userFileSystemStructure.Timestamp } );
            }
        }

        [Route("rename_folder")]
        [HttpPost]
        [ValidSessionRequired]
        public IActionResult RenameFolder(RenameFolderRequest renameFolderData)
        {
            Session? session = Request.RouteValues["Session"] as Session;


            if (session == null)
            {
                Response.StatusCode = 403;
                return new UnauthorizedResult();
            }

            UserFileSystemStructure? userFileSystemStructure = GlobalScope.database.UsersFileSystemStructures.Where(structure => structure.User == session.User).FirstOrDefault();

            if (userFileSystemStructure == null)
            {
                return new NotFoundResult();
            }

            FileSystemStructureFolder? rootFolder = JObject.Parse(userFileSystemStructure.SerializedStructure).ToObject<FileSystemStructureFolder>();

            if (rootFolder == null)
            {
                Response.StatusCode = 400;
                Console.WriteLine("Fail to load rootFolder");
                return new BadRequestResult();
            }

            FileSystemStructureFolder? thisFolder = JumpToFolder(rootFolder, renameFolderData.folderPath, false);

            if (thisFolder == null)
            {
                Response.StatusCode = 400;
                Console.WriteLine("Fail to find parentFolder");
                return new BadRequestResult();
            }

            thisFolder.FolderName = renameFolderData.newFolderName;

            userFileSystemStructure.SerializedStructure = JObject.FromObject(rootFolder).ToString();
            userFileSystemStructure.Timestamp = DateTime.Now;
            GlobalScope.database.UsersFileSystemStructures.Update(userFileSystemStructure);
            GlobalScope.database.SaveChangesAsync().Wait();

            return new ObjectResult(new LoadCloudStructure { Structure = rootFolder, LastEdit = userFileSystemStructure.Timestamp });

        }

        [Route("delete_folder")]
        [HttpPost]
        [ValidSessionRequired]
        public IActionResult DeleteFolder(DeleteFolderRequest deleteFolderData)
        {
            Session? session = Request.RouteValues["Session"] as Session;

            if (session == null)
            {
                Response.StatusCode = 403;
                return new UnauthorizedResult();
            }

            UserFileSystemStructure? userFileSystemStructure = GlobalScope.database.UsersFileSystemStructures.Where(structure => structure.User == session.User).FirstOrDefault();

            if (userFileSystemStructure == null)
            {
                return new NotFoundResult();
            }

            FileSystemStructureFolder? rootFolder = JObject.Parse(userFileSystemStructure.SerializedStructure).ToObject<FileSystemStructureFolder>();

            if (rootFolder == null)
            {
                Response.StatusCode = 400;
                Console.WriteLine("Fail to load rootFolder");
                return new BadRequestResult();
            }

            FileSystemStructureFolder? thisFolder = JumpToFolder(rootFolder, deleteFolderData.folderPath, false);

            if (thisFolder == null)
            {
                Response.StatusCode = 400;
                Console.WriteLine("Fail to find thisFolder: "+ deleteFolderData.folderPath);
                return new BadRequestResult();
            }
            Console.WriteLine("Deleting " + deleteFolderData.folderPath);
            RecursiveFoldersDelete(thisFolder);

            FileSystemStructureFolder? relativeFolder = JumpToFolder(rootFolder, deleteFolderData.folderPath.Substring(0, deleteFolderData.folderPath.LastIndexOf('\\')), false);

            if (relativeFolder == null)
            {
                Response.StatusCode = 400;
                Console.WriteLine("Fail to find relativeFolder: " + deleteFolderData.folderPath.Substring(0, deleteFolderData.folderPath.LastIndexOf('\\')));
                return new BadRequestResult();
            }

            userFileSystemStructure.SerializedStructure = JObject.FromObject(rootFolder).ToString();

            userFileSystemStructure.Timestamp = DateTime.Now;
            GlobalScope.database.UsersFileSystemStructures.Update(userFileSystemStructure);
            GlobalScope.database.SaveChangesAsync().Wait();

            return new ObjectResult( new LoadCloudStructure { Structure = rootFolder, LastEdit = userFileSystemStructure.Timestamp });
        }

        private void RecursiveFoldersDelete(FileSystemStructureFolder folder)
        {
            folder.Files.ToList().ForEach(file => {DeleteFileFromServer(file); folder.Files.Remove(file); });
            folder.Folders.ToList().ForEach(folder => { RecursiveFoldersDelete(folder); folder.Folders.Remove(folder); });
        }

        private void DeleteFileFromServer(FileSystemStructureFile file)
        {
            CloudFile? cloud_file = GlobalScope.database.CloudFiles.Where(cfile => cfile.ServersideToken == file.FileToken).FirstOrDefault();

            if (cloud_file == null)
            {
                Console.WriteLine("Failed to delete. file token not found");
                return;
            }

            GlobalScope.database.CloudFiles.Remove(cloud_file);
            FileInfo fileInfo = new FileInfo(@$"\cloud\{cloud_file.Userid}\{cloud_file.ServersideName}");
            if (fileInfo.Exists)
            {
                fileInfo.Delete();
            }
            else
            {
                Console.WriteLine("Failed to delete. file not found");
            }


        }

        [Route("create_folder")]
        [HttpPost]
        [ValidSessionRequired]
        public IActionResult CreateNewFolder(CreateFolderRequest newFolderData)
        {
            Session? session = Request.RouteValues["Session"] as Session;

            if (session == null)
            {
                Response.StatusCode = 403;
                return new UnauthorizedResult();
            }

            UserFileSystemStructure? userFileSystemStructure = GlobalScope.database.UsersFileSystemStructures.Where(structure => structure.User == session.User).FirstOrDefault();

            if (userFileSystemStructure == null)
            {
                return new NotFoundResult();
            }

            FileSystemStructureFolder? rootFolder = JObject.Parse(userFileSystemStructure.SerializedStructure).ToObject<FileSystemStructureFolder>();

            if (rootFolder == null)
            {
                Response.StatusCode = 400;
                Console.WriteLine("Fail to load rootFolder");
                return new BadRequestResult();
            }

            FileSystemStructureFolder? parentFolder = JumpToFolder(rootFolder, newFolderData.newFolderPath, false);

            Console.WriteLine(newFolderData.newFolderPath);

            if (parentFolder == null)
            {
                Response.StatusCode = 400;
                Console.WriteLine("Fail to find parentFolder");
                return new BadRequestResult();
            }

            parentFolder.Folders.Add(new FileSystemStructureFolder { FolderName = newFolderData.newFolderName });

            userFileSystemStructure.SerializedStructure = JObject.FromObject(rootFolder).ToString();
            userFileSystemStructure.Timestamp = DateTime.Now;
            GlobalScope.database.UsersFileSystemStructures.Update(userFileSystemStructure);
            GlobalScope.database.SaveChangesAsync().Wait();

            return new ObjectResult(new LoadCloudStructure { Structure = rootFolder, LastEdit = userFileSystemStructure.Timestamp });

        }


        [Route("rename_file")]
        [HttpPost]
        [ValidSessionRequired]
        public IActionResult RenameFile(RenameFileRequest renameFileData)
        {
            Session? session = Request.RouteValues["Session"] as Session;

            if (session == null)
            {
                Response.StatusCode = 403;
                return new UnauthorizedResult();
            }

            UserFileSystemStructure? userFileSystemStructure = GlobalScope.database.UsersFileSystemStructures.Where(structure => structure.User == session.User).FirstOrDefault();

            if (userFileSystemStructure == null)
            {
                return new NotFoundResult();
            }

            FileSystemStructureFolder? rootFolder = JObject.Parse(userFileSystemStructure.SerializedStructure).ToObject<FileSystemStructureFolder>();

            if (rootFolder == null)
            {
                Response.StatusCode = 400;
                return new BadRequestResult();
            }

            FileSystemStructureFolder? thisFolder = JumpToFolder(rootFolder, renameFileData.folderPath, false);

            if (thisFolder == null)
            {
                Response.StatusCode = 400;
                return new BadRequestResult();
            }

            FileSystemStructureFile? thisFile = thisFolder.Files.Where(sfiles => sfiles.FileName == renameFileData.fileName).FirstOrDefault();

            if (thisFile == null)
            {
                Response.StatusCode = 400;
                return new BadRequestResult();
            }

            thisFile.FileName = renameFileData.fileNewName;

            userFileSystemStructure.SerializedStructure = JObject.FromObject(rootFolder).ToString();
            userFileSystemStructure.Timestamp = DateTime.Now;
            GlobalScope.database.UsersFileSystemStructures.Update(userFileSystemStructure);
            GlobalScope.database.SaveChangesAsync().Wait();

            return new ObjectResult(new LoadCloudStructure { Structure = rootFolder, LastEdit = userFileSystemStructure.Timestamp });
        }


        [Route("delete_file")]
        [HttpPost]
        [ValidSessionRequired]
        public IActionResult DeleteFile(DeleteFileRequest deleteFileData)
        {
            Session? session = Request.RouteValues["Session"] as Session;

            if (session == null)
            {
                Response.StatusCode = 403;
                return new UnauthorizedResult();
            }

            UserFileSystemStructure? userFileSystemStructure = GlobalScope.database.UsersFileSystemStructures.Where(structure => structure.User == session.User).FirstOrDefault();

            if (userFileSystemStructure == null)
            {
                return new NotFoundResult();
            }

            FileSystemStructureFolder? rootFolder = JObject.Parse(userFileSystemStructure.SerializedStructure).ToObject<FileSystemStructureFolder>();

            if (rootFolder == null)
            {
                Response.StatusCode = 400;
                Console.WriteLine("Fail to find rootFolder");
                return new BadRequestResult();
            }

            FileSystemStructureFolder? thisFolder = JumpToFolder(rootFolder, deleteFileData.folderPath, false);

            if (thisFolder == null)
            {
                Response.StatusCode = 400;
                Console.WriteLine("Fail to find thisFolder " + deleteFileData.folderPath);
                return new BadRequestResult();
            }

            FileSystemStructureFile? thisFile = thisFolder.Files.Where(sfiles => sfiles.FileName == deleteFileData.fileName).FirstOrDefault();

            if (thisFile == null)
            {
                Response.StatusCode = 400;
                Console.WriteLine("Fail to find thisFile");
                return new BadRequestResult();
            }

            DeleteFileFromServer(thisFile);
            thisFolder.Files.Remove(thisFile);

            userFileSystemStructure.SerializedStructure = JObject.FromObject(rootFolder).ToString();
            userFileSystemStructure.Timestamp = DateTime.Now;
            GlobalScope.database.UsersFileSystemStructures.Update(userFileSystemStructure);
            GlobalScope.database.SaveChangesAsync().Wait();

            return new ObjectResult(new LoadCloudStructure { Structure = rootFolder, LastEdit = userFileSystemStructure.Timestamp });
        }

        [Route("create_file")]
        [HttpPost]
        [ValidSessionRequired]
        public IActionResult CreateNewFile([FromForm] CreateFileRequest newFileData)
        {
            Session? session = Request.RouteValues["Session"] as Session;

            if (session == null)
            {
                Response.StatusCode = 403;
                return new UnauthorizedResult();
            }

            UserFileSystemStructure? userFileSystemStructure = GlobalScope.database.UsersFileSystemStructures.Where(structure => structure.User == session.User).FirstOrDefault();

            if (userFileSystemStructure == null)
            {
                return new NotFoundResult();
            }

            FileSystemStructureFolder? rootFolder = JObject.Parse(userFileSystemStructure.SerializedStructure).ToObject<FileSystemStructureFolder>();

            if (rootFolder == null)
            {
                Response.StatusCode = 400;
                Console.WriteLine("Fail to find rootFolder");
                return new BadRequestResult();
            }

            FileSystemStructureFolder? parentFolder = JumpToFolder(rootFolder, newFileData.folderPath, false);

            if (parentFolder == null)
            {
                Response.StatusCode = 400;
                Console.WriteLine("Fail to find folderPath " + newFileData.folderPath);
                return new BadRequestResult();
            }

            string path = @Directory.GetCurrentDirectory() + @$"\cloud\{session.User.Id}";

            Directory.CreateDirectory(path);


            var serverside_name = Guid.NewGuid().ToString().Replace("-", "");
            path += @$"\{serverside_name}";

            using (var stream = System.IO.File.Create(path))
            {
                newFileData.File.CopyTo(stream);
            }

            FileSystemStructureFile newSystemStructureFile = new FileSystemStructureFile
            {
                FileName = newFileData.fileName,
                FileLenght = newFileData.File.Length,
                FileToken = Guid.NewGuid().ToString().Replace("-", ""),
            };

            parentFolder.Files.Add(newSystemStructureFile);

            GlobalScope.database.CloudFiles.Add(new CloudFile
            {
                User = session.User,
                Lenght = newSystemStructureFile.FileLenght,
                ServersideName = serverside_name.ToString().Replace("-", ""),
                ServersideToken = newSystemStructureFile.FileToken
            });
                    

            userFileSystemStructure.SerializedStructure = JObject.FromObject(rootFolder).ToString();
            userFileSystemStructure.Timestamp = DateTime.Now;
            GlobalScope.database.UsersFileSystemStructures.Update(userFileSystemStructure);


            GlobalScope.database.SaveChangesAsync().Wait();

            return new ObjectResult(new LoadCloudStructure { Structure = rootFolder, LastEdit = userFileSystemStructure.Timestamp });
            
        }

        [Route("download_file")]
        [HttpPost]
        [ValidSessionRequired]
        public IActionResult DownloadFile(DownloadFileRequest downloadingFileData)
        {
            Session? session = Request.RouteValues["Session"] as Session;

            if (session == null)
            {
                Response.StatusCode = 400;
                return new BadRequestResult();
            }

            UserFileSystemStructure? userFileSystemStructure = GlobalScope.database.UsersFileSystemStructures.Where(structure => structure.User == session.User).FirstOrDefault();

            if (userFileSystemStructure == null)
            {
                Response.StatusCode = 204;
                return new BadRequestResult(); 
            }

            FileSystemStructureFolder? rootFolder = JObject.Parse(userFileSystemStructure.SerializedStructure).ToObject<FileSystemStructureFolder>();

            if (rootFolder == null)
            {
                Response.StatusCode = 500;
                return new BadRequestResult();
            }

            FileSystemStructureFolder? parentFolder = JumpToFolder(rootFolder, downloadingFileData.folderPath, false);

            if (parentFolder == null)
            {
                Response.StatusCode = 500;
                return new BadRequestResult();
            }

            FileSystemStructureFile? file = parentFolder.Files.Find(file => file.FileToken == downloadingFileData.fileToken);

            if (file == null)
            {
                Response.StatusCode = 500;
                return new BadRequestResult();
            }

            CloudFile? cloudFile = GlobalScope.database.CloudFiles.Where(file => file.User == session.User && file.ServersideToken == downloadingFileData.fileToken).FirstOrDefault();

            if (cloudFile == null)
            {
                Response.StatusCode = 500;
                return new BadRequestResult();
            }

            var filePath = @Directory.GetCurrentDirectory() + @$"\cloud\{session.User.Id}\{cloudFile.ServersideName}";


            return PhysicalFile(filePath, "application/octet-stream", file.FileName);
        }


        private static FileSystemStructureFolder? JumpToFolder(FileSystemStructureFolder rootFolder, string folderPath, bool updateEditTime)
        {
            FileSystemStructureFolder? currentFolder = rootFolder;

            if (folderPath.Length > 0 && folderPath[0] == '\\')
            {
                folderPath = folderPath.Remove(0, 1);
            }

            if (folderPath.Length == 0)
            {
                if (updateEditTime)
                    rootFolder.FolderEditTime = DateTime.Now;
                return rootFolder;
            }

            var folders = folderPath.Split('\\');

            foreach (var folder in folders)
            {
                if (updateEditTime)
                    currentFolder.FolderEditTime = DateTime.Now;

                currentFolder = currentFolder.Folders.Find(_folder => _folder.FolderName == folder);

                if (currentFolder == null)
                    return null;
            }

            return currentFolder;
        }

        [Route("test_upload")]
        [HttpPost]
        public string Upload([FromForm] CreateFileRequest request)
        {
            string path = @Directory.GetCurrentDirectory() + @$"\cloud\test\{request.folderPath}";

            Directory.CreateDirectory(path);

            path += @$"\{request.fileName}";

            using (var stream = System.IO.File.Create(path))
            {
                request.File.CopyTo(stream);
            }

            return @path;
        }

        [Route("test_download")]
        [HttpPost]
        public PhysicalFileResult Download(DownloadTestFileRequest request)
        {
            string path = @Directory.GetCurrentDirectory() + @$"\cloud\test\{request.folderPath}\{request.fileName}";

            return PhysicalFile(path, "application/octet-stream", request.fileName+".txt");
        }
    }
    public class DownloadTestFileRequest 
    {
        [Required]
        public string folderPath { get; set; } = null!;
        [Required]
        public string fileName { get; set; } = null!;
    }

    public class DownloadFileRequest
    {
        [Required]
        public string folderPath { get; set; } = null!;
        [Required]
        public string fileToken { get; set; } = null!;
    }

    
    public class DeleteFileRequest
    {
        [Required]
        public string folderPath { get; set; } = null!;
        [Required]
        public string fileName { get; set; } = null!;
    }

    public class RenameFileRequest
    {
        [Required]
        public string folderPath { get; set; } = null!;
        [Required]
        public string fileName { get; set; } = null!;
        [Required]
        public string fileNewName { get; set; } = null!;
    }

    public class CreateFileRequest
    {
        [Required]
        public string folderPath { get; set; } = null!;
        [Required]
        public string fileName { get; set; } = null!;
        [Required]
        public IFormFile File { get; set; } = null!;
    }

    public class RenameFolderRequest
    {
        [Required]
        public string folderPath { get; set; } = null!;
        [Required]
        public string newFolderName { get; set; } = null!;
    }

    public class DeleteFolderRequest
    {
        [Required]
        public string folderPath { get; set; } = null!;
    }

    public class CreateFolderRequest
    {
        [Required]
        public string newFolderPath { get; set; } = null!;

        [Required]
        public string newFolderName { get; set; } = null!;

    }


    public class LoadCloudStructure
    {
        public FileSystemStructureFolder? Structure { get; set; }

        public DateTime LastEdit { get; set; } = DateTime.Now;
    }




    public class FileSystemStructureFolder
    {
        public string FolderName { get; set; } = String.Empty;

        public DateTime FolderEditTime { get; set; } = DateTime.Now;

        public List<FileSystemStructureFolder> Folders { get; set; } = new List<FileSystemStructureFolder>();

        public List<FileSystemStructureFile> Files { get; set; } = new List<FileSystemStructureFile>();
    }

    public class FileSystemStructureFile
    {
        public string FileName { get; set; } = String.Empty;
        public DateTime FileEditTime { get; set; } = DateTime.Now;
        public string FileHash { get; set; } = String.Empty;
        public long FileLenght { get; set; } = 0;
        public string FileToken { get; set; } = String.Empty;
    }

}
