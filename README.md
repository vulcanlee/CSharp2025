# C# 專案範例原始碼

|專案名稱|專案說明|備註|
|-|-|-|
|*csNET9WebApiStandard|ASP.NET Core 9 Web API 專案範本||
|csAzureBlobStorage|如何使用與存取 Azure Blob Storage Service||
|csSpeechToTextBatch|將語音檔案進行批次轉錄成為文字||
|csSTTAllInOne|設機一個支援類別，整理上傳 .mp3 與 轉文字的功能||
|csDicomToImage|將 DICOM 轉換成為一個 Image||
|csDicomListAllTag|列出 DICOM 檔案中的所有標籤||
|csDicomGetSetTagValue|取得或設定 DICOM 檔案中的標籤值||
|csBlazor|使用Github Copilot 輔助開發網頁||
|MonitorTool|||
|csBlazorAntDesignQuickStart|使用 Ant Design Blazor 組件快速建立頁面||
|csBlazorRoutingParameter|Blazor 跳轉 1 : 動態路由參數，傳遞與接收路由參數||
|csBlazorQueryString|Blazor 跳轉 2 : 導航切換頁面時候，透過查詢字串，傳遞與接收查詢參數||
|csPatientCRUD|病人資料 CRUD 操作範例||
|ReactWebApi|前端使用 React，後端使用 ASP.NET Core 的測試標準專案||
|reactGet|React API 01 : 呼叫一個 Get 方法 API，並將結果渲染到網頁上||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||
||||

iOS Simulator: A fatal error occurred while trying to start the server.

xcrun simctl shutdown all

rm -r ~/Library/Developer/CoreSimulator/Caches

sudo rm -R /Users/swee/Library/Developer/CoreSimulator/Caches

open -a Simulator

cat /Library/Logs/CoreSimulator/CoreSimulator.log

On macOS 13 and above
Go to System Settings → General → Storage → Developer
Delete "Developer Caches"
