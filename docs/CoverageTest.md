カバレッジテスト実施手順
========================

事前準備
--------

以下のツールが必要。

* NUnit 3
* OpenCover: https://github.com/OpenCover/opencover
* ReportGenerator: http://www.palmmedia.de/OpenSource/ReportGenerator

ReportGenerator は c:\ICF_AutoCapsule_disabled\ReportGenerator に
インストールしておくこと。

coverage.bat ファイルに、上記3ツールのディレクトリを指定しておくこと。

カバレッジテスト実施
--------------------

Visual Studio を起動し、フルビルドを行っておく。

coverage.bat を起動すると、OpenCover / NUnit が起動されテストが実行される。
その後、ReportGenerator により HTML レポートが CoverageReport ディレクトリ
に出力される。

実行されるのは Nebula.Test 側のみである。
Nebula.IT 側を実行したい場合は、coverage.bat の target= の値を変更すること。
