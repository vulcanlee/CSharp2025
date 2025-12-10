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
|ReactWebApi|React API 01 : 前端使用 React，後端使用 ASP.NET Core 的測試標準專案||
|reactGet|React API 02 : 呼叫一個 Get 方法 API，並將結果渲染到網頁上||
|reactGetQueryString|React API 03 : 呼叫一個 Get 方法 API，取得查詢字串內容並將結果渲染到網頁上||
|reactGetRoutingValue|React API 04 : 呼叫一個 Get 方法 API，取得路由內容並將結果渲染到網頁上||
|ReactGetHeader|React API 05 : 呼叫一個 Get 方法 API，傳送與取得 Header 的數值||
|ReactGetCookie|React API 06 : 呼叫一個 Get 方法 API，傳送與取得 Cookie 的數值||
|ReactPostJson|React API 07 : 呼叫一個 Post 方法 API，傳送 JSON 資料並取得回應||
|csBlazorGoogleOAuth2|Blazor 使用 Google OAuth 2.0||
|csBlazorSiderBar|Ant Design Blazor 側邊欄範例||
|csBlazorCustomTheme|Ant Design Blazor 自訂主題範例||
|ReactCrudRetrive|React CRUD 之 取得清單||
|*GithubCopilot1|Github Copilot 1 : 透過ChatGPT將畫面直接產生出 Blazor 頁面||
|*GithubCopilot2|Github Copilot 2 : 透過Copilot將畫面直接產生出 Blazor 頁面||
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
