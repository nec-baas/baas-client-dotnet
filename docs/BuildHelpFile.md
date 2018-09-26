リファレンスの生成手順
======================

1) SHFB (SandCasle Help File Builder) を以下からダウンロード・インストールする。

    https://github.com/EWSoftware/SHFB

2) Visual Studio でプロジェクトをビルドしておく。

3) src/NebulaOfflineDocs.shfbproj ファイルを SHFB で開く。

4) Build 実行。

ヘルプファイルは Help 以下に生成される。


トラブルシューティング
======================

BB0065 : cast エラーが出る場合
------------------------------

プロジェクトの packages 以下に SHFB の古いバイナリがないか確認。

BE0070 エラーが出る場合
-----------------------

Framework が違うというエラー。core側(portable)で発生する。
.csproj ファイルではなく、ビルド済みの .dll/.xml から生成すればよい。

BE0040 エラーが出る場合
-----------------------

Visual Studio でビルドを実施してから HTML 生成すること。


補足
=====

NebulaOfflineDocs から NebulaCoreDocs を参照しているが、この設定は Plug-Ins の "Version Builder" で行っている。

Namespaceを複数出力しようとしてもうまくいかない。これは VS2013 Presentation Style の不具合の模様。
https://stackoverflow.com/questions/25198190/sandcastle-html-output-multiple-namespaces
これに対応するため Conceptual Content を追加している。
