using System.Text.RegularExpressions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace PvZOL.Server
{
    public partial class PvzFileProvider : IFileProvider
    {
        private readonly IFileProvider m_inner;
        
        public PvzFileProvider(IFileProvider inner)
        {
            m_inner = inner;
        }
        
        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            throw new Exception("can't");
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            var phys = m_inner.GetFileInfo(subpath);
            if (phys.Exists) return phys;

            var match = s_md5Regex.Match(subpath);
            if (!match.Success)
            {
                return new NotFoundFileInfo(subpath);
            }

            var extension = Path.GetExtension(subpath);
            return m_inner.GetFileInfo($"{match.Groups[1].ValueSpan}{extension}");
        }

        [GeneratedRegex("^(.*)_[a-f0-9]{32}\\.(?:swf|bl|bbone)$")]
        private partial Regex s_md5Regex { get; }

        public IChangeToken Watch(string filter)
        {
            throw new Exception("can't");
        }
    }
}