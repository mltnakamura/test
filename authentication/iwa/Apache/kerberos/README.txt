

■AD側環境設定
---------------------------------------------------------------
(Ⅰ)．ActiveDirectoryにユーザ追加
---------------------------------------------------------------

	ドメインの認証用ユーザとして、ダミーアカウントを作成します。
	一般ユーザー（Domain Users）として作成します。

	ユーザー名：任意。		<--- ①	（例：kerbneuron）
	パスワード：任意。（↓）<--- ②	（例：Neuro1NaPa2chEKerb123&）
	
	大文字小文字を入り交えること

	このアカウントは、統合認証のためだけに使用されるので、ユーザー登録時と
	この後の認証ファイル作成時以外はパスワードを入力する事がありません。
	よって、パスワードは長くして堅牢性を持たせるようにしたほうが良いです。
	簡単なパスワードだとKeytabファイルを作成できない


---------------------------------------------------------------
(Ⅱ)．統合認証用keytabファイル作成（KERBEROS認証）
---------------------------------------------------------------

	ktpass.exe コマンドにより、サービスプリンシパルをダミーアカウントに
	マップするkeytabファイルを作成します。

	ktpassの各オプションには、以下を指定してください。（< > 内を環境に合わせて設定）
	-out <出力するkeytabファイルのパスとファイル名>
	-princ HTTP/<ApacheサーバのURL[FQDN]>@<大文字ドメイン名>
	-mapuser <Ⅰ-①で作成したユーザ名>@<大文字ドメイン名>
	-crypto RC4-HMAC-NT 
	-ptype KRB5_NT_PRINCIPAL 
	-pass <Ⅰ-②のパスワードを設定>

	以下にktpassコマンドの入力例を示します。
	例）※注意：コマンドは１行で入力するようにします。
	
	C:\> ktpass.exe -out C:\work\kerbneuron.keytab -princ HTTP/neuron-server@CORP.EXAMPLE.COM  -mapuser kerbneuron@CORP.EXAMPLE.COM -crypto RC4-HMAC-NT -ptype KRB5_NT_PRINCIPAL -pass Neuro1NaPa2chEKerb123&


■Apache側環境設定
---------------------------------------------------------------
mod_auth_kerbインストール（ケルベロス認証用モジュール）
---------------------------------------------------------------

 centosの場合
	sudo yum install mod_auth_kerb

 ubuntuの場合
	apt-get install libapache2-mod-auth-kerb

---------------------------------------------------------------
Apache　サーバ　/etc/krb5.conf　の内容を編集
---------------------------------------------------------------
[logging]
 default = FILE:/var/log/krb5libs.log
 kdc = FILE:/var/log/krb5kdc.log
 admin_server = FILE:/var/log/kadmind.log

[libdefaults]
 default_realm = CORP.EXAMPLE.COM
 dns_lookup_realm = false
 dns_lookup_kdc = false
 ticket_lifetime = 60m
 renew_lifetime = 1d
 forwardable = true

[realms]
 CORP.EXAMPLE.COM = {
　## ↓認証ADサーバ（IPアドレス）を指定（port:88）
  kdc = 192.168.1.201:88
 }

[domain_realm]
 .corp.example.com = CORP.EXAMPLE.COM
 corp.example.com = CORP.EXAMPLE.COM


---------------------------------------------------------------
Apache　サーバ　/etc/httpd/conf.d/auth_kerb.conf　の内容を編集
---------------------------------------------------------------
<Location />
#  Kerberos Setting
  AuthType Kerberos
  AuthName "Kerberos Login"
  ## ↓統合Windows認証の場合Webサイトを登録（keytabを作った時のprincの＠より前と一致させる必要がある）
  KrbServiceName HTTP/neuron-server

  KrbMethodNegotiate On					## ←　統合認証　On
  KrbMethodK5Passwd Off					## ←　Basic認証　Off（Onの場合統合認証に失敗するとベーシック認証になる）
  KrbAuthRealms CORP.EXAMPLE.COM
  KrbVerifyKDC   Off
  require valid-user
#  SSLRequireSSL
  ## ↓統合Windows認証の場合keytabファイルの配置場所を登録
  Krb5KeyTab /etc/httpd/conf/kerbneuron.keytab

#  Cookie Setting
  RewriteEngine on
  RewriteCond %{REMOTE_USER} (.*)
  RewriteRule .* - [E=REMOTE_USER:%1]
  Header add Set-Cookie "uid=%{REMOTE_USER}e; path=/"
</Location>

---------------------------------------------------------------
WindowsADサーバー上で作成したkeytabファイルをLinuxの/etc/httpd/confへコピー
---------------------------------------------------------------
	sudo cp kerbneuron.keytab /etc/httpd/conf
	cd /etc/httpd/conf
	sudo chown apache:apache kerbneuron.keytab
	sudo chmod 640 kerbneuron.keytab

---------------------------------------------------------------
Apache再起動
---------------------------------------------------------------
	sudo service httpd restart


■クライアントPCブラウザ環境設定
-----------------------
IEの場合
-----------------------
□統合認証ON
	インターネットオプション
		↓
	詳細設定
		↓
	セキュリティ　統合Windows 認証を使用する　⇒ チェックON（デフォルト）

□イントラサイト追加
	インターネットオプション
		↓
	セキュリティタブ
		↓
	ローカルイントラネット
		↓
	サイト追加（http://192.168.1.201）
	（認証を受けさせるWebサイトを登録しないとユーザー/パスワード入力ダイアログが表示される）

-----------------------
Firefoxの場合
-----------------------
	Windows環境において統合Windows認証を利用したイントラネットサイトを開く場合、IEでは自動で認証が通るが、
	Firefoxではダイヤログボックスが出てきてユーザー名、パスワードを入力する必要がある。

	但しFirefoxでもこの統合Windows認証には対応しており、次の設定により自動化を有効に出来る。

	□ロケーションバーに”about:config"と入力し、Enterキーを押す。
	□以下のプロパティを探し、それぞれダブルクリックして、統合Windows認証を行っているサイトのURLを入力する。
		'network.automatic-ntlm-auth.trusted-uris' 
		'network.negotiate-auth.delegation-uris' 
		'network.negotiate-auth.trusted-uris' 

		サイトのURLの例）	http://192.168.1.201

