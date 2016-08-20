using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectronicObserver.Observer.Cache
{
	static class TaskRecord
	{
		static ConcurrentDictionary<int, string> record = new ConcurrentDictionary<int, string>();
		//KEY: id+url, Value: filepath
		//只有在验证文件修改时间后，向客户端返回本地文件或者将文件保存到本地时才需要使用

		static public void Add( int tkey, string filepath )
		{
			record.AddOrUpdate( tkey, filepath, ( key, oldValue ) => filepath );
		}

		static public string GetAndRemove( int tkey )
		{
			string ret;
			record.TryRemove( tkey, out ret );
			return ret;
		}
		static public string Get( int tkey )
		{
			string ret;
			if ( record.TryGetValue( tkey, out ret ) )
				return ret;
			return "";
		}
	}
}
