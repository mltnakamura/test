################################################################
 Apache <==> ActiveDirectory の LDAP認証連携
 Oracle Linux Server 5.8
 Apache httpd 2.2.3-65.0.1
 AvtiveDirectory(LDAP) Windows 2008
################################################################

■LDAP連携モジュールの準備
※Apache 2.2 の為 mod_authz_ldap を使用
	sudo yum list | grep ldap にて確認

		mod_authz_ldap.x86_64       0.26-11.el5               installed 
		mozldap.x86_64              6.0.5-1.el5               installed 
		nss_ldap.i386               253-49.el5                installed 
		nss_ldap.x86_64             253-49.el5                installed 
		openldap.i386               2.3.43-25.el5_8.1         installed 
		openldap.x86_64             2.3.43-25.el5_8.1         installed 
		python-ldap.x86_64          2.2.0-2.1                 installed 


	□mod_authz_ldapがインストールされていない場合
		sudo yun install mod_authz_ldap


■LDAP 連携の設定をhttpd.confに追記
	sudo vi /etc/httpd/conf/httpd.conf

------------ ここから↓ --------------------------------------
LoadModule authz_user_module modules/mod_authz_user.so
LoadModule ldap_module modules/mod_ldap.so
LoadModule authnz_ldap_module modules/mod_authnz_ldap.so

<Location />
  AuthType Basic
  AuthName "Neuron LDAP Auth"
  AuthBasicProvider ldap
  AuthLDAPURL "ldap://NEURON-AD.CORP.EXAMPLE.COM:389/OU=japan,DC=CORP,DC=EXAMPLE,DC=COM?sAMAccountName?sub?(objectClass=*)"
  AuthLDAPBindDN        "tuser01@CORP.EXAMPLE.COM"
  AuthLDAPBindPassword  "neuron123&"
  AuthzLDAPAuthoritative Off
  AuthLDAPGroupAttributeIsDN on
  Require ldap-group CN=neuron,OU=japan,DC=CORP,DC=EXAMPLE,DC=COM
#  Require valid-user

#  Cookie Setting for Neuron
  RewriteEngine on
  RewriteCond %{REMOTE_USER} (.*)
  RewriteRule .* - [E=REMOTE_USER:%1]
  Header add Set-Cookie "uid=%{REMOTE_USER}e@CORP.EXAMPLE.COM; path=/"

</Location>

------------ ここまで↑ 以下↓説明--------------------------------------

LoadModule authz_user_module modules/mod_authz_user.so			<-- 認証用モジュール（既にhttpd.conf上部でロードされていれば記述不要）
LoadModule ldap_module modules/mod_ldap.so						<-- 認証用モジュール（既にhttpd.conf上部でロードされていれば記述不要）
LoadModule authnz_ldap_module modules/mod_authnz_ldap.so		<-- 認証用モジュール（既にhttpd.conf上部でロードされていれば記述不要）

<Location />
  AuthType Basic                                                    <-- Basic認証
  AuthName "Neuron LDAP Auth"                                       <-- Basic認証のダイアログに表示されるタイトル
  AuthBasicProvider ldap                                            <-- 認証にLDAP認証を使用する
  AuthLDAPURL "ldap://NEURON-AD.CORP.EXAMPLE.COM:389/OU=japan,DC=CORP,DC=EXAMPLE,DC=COM?sAMAccountName?sub?(objectClass=*)"
                            ↑--NEURON-AD.CORP.EXAMPLE.COM:389	AvtiveDirectoryサーバのFQDNとポート番号（389:デフォルト）
                            ↑--OU=japan                        検索対象のOU（組織）:ADの場合OUの指定が必要のようだ。
                            ↑--DC=CORP,DC=EXAMPLE,DC=CO        検索対象のドメイン（CORP.EXAMPLE.COM）
                            ↑--?sAMAccountName                 アカウント名を示す属性（ADの場合はuidでなくsAMAccountNameを使用）
                            ↑--?sub?(objectClass=*)            サブツリーも検索対象で、全てのobjectClassが対象
  AuthLDAPBindDN        "tuser01@CORP.EXAMPLE.COM"					<-- admin権限が無いユーザーでもOK
  AuthLDAPBindPassword  "neuron123&"								<-- 上記ユーザーのパスワード
  AuthzLDAPAuthoritative Off                                        <-- Off:LDAP以外の認証は組み合わせない
  AuthLDAPGroupAttributeIsDN on                                     <-- On:グループメンバシップをチェックする際にユーザ名の識別名を使用する
  Require ldap-group CN=neuron,OU=japan,DC=CORP,DC=EXAMPLE,DC=COM	<-- 認証をグループで制御したい場合のDN指定
                                                                        ※この場合CORP.EXAMPLE.COMドメインのOU=japan内の
                                                                          CN=neuronグループのメンバが認証可能。
                                                                          メンバ以外は認証でエラーとなる。
#  Require valid-user												<-- OU以下すべてのユーザーが認証対象となる。

#  Cookie Setting for Neuron										<-- Neuron 用ユーザー識別用クッキー生成
  RewriteEngine on
  RewriteCond %{REMOTE_USER} (.*)
  RewriteRule .* - [E=REMOTE_USER:%1]
  Header add Set-Cookie "uid=%{REMOTE_USER}e@CORP.EXAMPLE.COM; path=/"	<-- ドメイン名が取得できない為、固定値で書く

</Location>
------------ ここまで↑ ↓以下サンプルのActiveDirectoryの構造----------------------
 ex)    
    CORP.EXAMPLE.COM
        OU=japan
            |-- OU=tokyo
            |     |--- tuser01 (group=neuron)  <-- グループで認証の場合このユーザは認証OK
            |     |--- tuser02
            |
            |-- OU=osaka
                  |--- ouser01 (group=neuron)  <-- グループで認証の場合このユーザは認証OK
                  |--- ouser02

------------------------------------------------------------------------------------

【参考】Active Directoryオブジェクトの識別名（DN）を確認するコマンド
          dsquery コマンド（AD上で実施）
　ex)
	C:\>dsquery user -name tuser01
	"CN=tuser01,OU=tokyo,OU=japan,DC=CORP,DC=EXAMPLE,DC=COM"

	C:\>dsquery user -name ouser01
	"CN=ouser01,OU=osaka,OU=japan,DC=CORP,DC=EXAMPLE,DC=COM"

	C:\>dsquery group -name neuron
	"CN=neuron,OU=japan,DC=CORP,DC=EXAMPLE,DC=COM"


【参考】
   Apche2.4 mod_authnz_ldap　でグローバルカタログ（port:3268）を認証検索対象にした場合、
　　AD内のすべてのユーザーを対象にすることも可能。その場合の設定は以下のようになるはず。
　（ただし、検証環境では接続できず未検証）

  AuthLDAPURL "ldap://NEURON-AD.CORP.EXAMPLE.COM:3268/?userPrincipalName?sub"
  AuthLDAPBindDN "administrator@CORP.EXAMPLE.COM"
  AuthLDAPBindPassword  "neuron123&"
  AuthzLDAPAuthoritative Off
  Require valid-user

　http://httpd.apache.org/docs/2.4/mod/mod_authnz_ldap.html#activedirectory



