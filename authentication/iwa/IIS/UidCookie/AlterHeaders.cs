using System; 
using System.Web; 
using System.Configuration;

namespace MyIisExtentionModure 
{ 
    public class AlterHeaders : IHttpModule 
    { 
        public void Dispose() {}

        public void Init(HttpApplication context) 
        { 
            //ヘッダー送信前前のイベントハンドラを定義 
            context.PreSendRequestHeaders += OnPreSendRequestHeaders; 
        }

        void OnPreSendRequestHeaders(object sender, EventArgs e) 
        { 
        	// サーバ変数の"REMOTE_USER"を取得
		//string headerValue = "uid=user3@CORP.EXAMPLE.COM; path=/";
		String headerValue = HttpContext.Current.Request.ServerVariables["REMOTE_USER"];
		String headerValue2 = headerValue.Replace('\\','#') + "#";
		String[] aryRemoteUser = headerValue2.Split('#');

		//HTTP応答ヘッダーに "Set-Cookie" を追加　ドメインは"@CORP.EXAMPLE.COM"
		if(aryRemoteUser.Length > 1){
			HttpContext.Current.Response.Headers.Set("Set-Cookie", "uid="  + aryRemoteUser[1] + "@CORP.EXAMPLE.COM; path=/");
		}
		HttpContext.Current.Response.Headers.Set("DBG_REMOTE_USER", headerValue2);	// テスト用
        } 
    } 
} 
