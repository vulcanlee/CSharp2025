import { useEffect, useState } from 'react';

const API = 'http://localhost:5281';

interface UserInfo {
    isAuthenticated: boolean;
    name?: string;
    email?: string;
}

interface SecureData {
    secret: string;
    at: string;
}

function App() {
    const [me, setMe] = useState<UserInfo | null>(null);
    const [secureData, setSecureData] = useState<SecureData | null>(null);
    const [loading, setLoading] = useState<boolean>(true);
    const [error, setError] = useState<string | null>(null);

    // 修復 Permissions API 綁定問題（防禦性代碼）
    useEffect(() => {
        // 確保 Permissions.query 方法綁定正確
        if (navigator.permissions && typeof navigator.permissions.query === 'function') {
            const originalQuery = navigator.permissions.query;
            navigator.permissions.query = function (descriptor: PermissionDescriptor) {
                return originalQuery.call(navigator.permissions, descriptor);
            };
        }

        // 全局錯誤處理
        const handleError = (event: ErrorEvent) => {
            if (event.error?.message?.includes('Illegal invocation')) {
                console.warn('捕獲到 Illegal invocation 錯誤，已忽略:', event.error);
                event.preventDefault(); // 防止錯誤中斷應用
            }
        };

        window.addEventListener('error', handleError);
        return () => window.removeEventListener('error', handleError);
    }, []);

    // 載入當前用戶狀態
    const loadMe = async () => {
        try {
            setLoading(true);
            setError(null);
            const res = await fetch(`${API}/api/me`, {
                credentials: 'include',
                headers: {
                    'Accept': 'application/json'
                }
            });

            if (!res.ok) {
                throw new Error(`HTTP error! status: ${res.status}`);
            }

            const data = await res.json();
            setMe(data);
        } catch (err) {
            console.error('載入用戶狀態失敗:', err);
            setError('無法載入用戶狀態');
            setMe({ isAuthenticated: false });
        } finally {
            setLoading(false);
        }
    };

    // 初始化時載入用戶狀態
    useEffect(() => {
        loadMe();
    }, []);

    // Google 登入
    const login = () => {
        // 儲存當前頁面狀態(如果需要)
        sessionStorage.setItem('preLoginPath', window.location.pathname);
        // 導向後端 /login 端點，觸發 Google OAuth2 流程
        window.location.href = `${API}/do-login`;
    };

    // 登出
    const logout = async () => {
        try {
            setError(null);
            const res = await fetch(`${API}/logout`, {
                method: 'POST',
                credentials: 'include'
            });

            if (!res.ok) {
                throw new Error(`登出失敗: ${res.status}`);
            }

            // 清除本地狀態
            setMe({ isAuthenticated: false });
            setSecureData(null);

            // 重新載入用戶狀態
            await loadMe();
        } catch (err) {
            console.error('登出失敗:', err);
            setError('登出失敗，請稍後再試');
        }
    };

    // 呼叫受保護的 API
    const callSecureApi = async () => {
        try {
            setError(null);
            const res = await fetch(`${API}/api/secure/data`, {
                credentials: 'include',
                headers: {
                    'Accept': 'application/json'
                }
            });

            if (res.status === 401) {
                setError('未登入或 Cookie 已過期，請重新登入');
                setMe({ isAuthenticated: false });
                return;
            }

            if (!res.ok) {
                throw new Error(`HTTP error! status: ${res.status}`);
            }

            const data = await res.json();
            setSecureData(data);
        } catch (err) {
            console.error('呼叫 API 失敗:', err);
            setError('無法取得受保護的資料');
        }
    };

    // 測試 API
    const testApi = async () => {
        try {
            const res = await fetch('https://your-api-host/api/dev/token', { credentials: 'include' });
            const data = await res.json();
            console.log('API 測試回應:', data);
        } catch (err) {
            console.error('API 測試失敗:', err);
        }
    };

    // 載入中狀態
    if (loading) {
        return (
            <div style={{ padding: 24 }}>
                <h1>React + ASP.NET Core 9 + Google OAuth2 (BFF)</h1>
                <p>載入中...</p>
            </div>
        );
    }

    return (
        <div style={{ padding: 24, fontFamily: 'Arial, sans-serif' }}>
            <h1>React + ASP.NET Core 9 + Google OAuth2 (BFF)</h1>

            {/* 錯誤訊息顯示 */}
            {error && (
                <div style={{
                    padding: 12,
                    marginBottom: 16,
                    backgroundColor: '#fee',
                    border: '1px solid #fcc',
                    borderRadius: 4,
                    color: '#c00'
                }}>
                    <strong>錯誤:</strong> {error}
                </div>
            )}

            {/* 認證操作區 */}
            <section style={{ marginBottom: 24 }}>
                <h2>認證操作</h2>
                {!me?.isAuthenticated ? (
                    <button
                        onClick={login}
                        style={{
                            padding: '10px 20px',
                            fontSize: '16px',
                            backgroundColor: '#4285f4',
                            color: 'white',
                            border: 'none',
                            borderRadius: 4,
                            cursor: 'pointer'
                        }}
                    >
                        🔐 使用 Google 登入
                    </button>
                ) : (
                    <button
                        onClick={logout}
                        style={{
                            padding: '10px 20px',
                            fontSize: '16px',
                            backgroundColor: '#dc3545',
                            color: 'white',
                            border: 'none',
                            borderRadius: 4,
                            cursor: 'pointer'
                        }}
                    >
                        🚪 登出
                    </button>
                )}
            </section>

            {/* 用戶狀態顯示 */}
            <section style={{
                marginBottom: 24,
                padding: 16,
                backgroundColor: '#f5f5f5',
                borderRadius: 4
            }}>
                <h2>目前登入狀態</h2>
                {me?.isAuthenticated ? (
                    <div>
                        <p>✅ <strong>已登入</strong></p>
                        <p><strong>姓名:</strong> {me.name}</p>
                        <p><strong>Email:</strong> {me.email}</p>
                    </div>
                ) : (
                    <p>❌ <strong>未登入</strong></p>
                )}
                <details style={{ marginTop: 12 }}>
                    <summary style={{ cursor: 'pointer' }}>查看原始資料</summary>
                    <pre style={{
                        marginTop: 8,
                        padding: 12,
                        backgroundColor: '#fff',
                        border: '1px solid #ddd',
                        borderRadius: 4,
                        overflow: 'auto'
                    }}>
                        {JSON.stringify(me, null, 2)}
                    </pre>
                </details>
            </section>

            {/* 受保護 API 存取區 */}
            <section style={{
                padding: 16,
                backgroundColor: '#f5f5f5',
                borderRadius: 4
            }}>
                <h2>受保護的 API</h2>
                <button
                    onClick={callSecureApi}
                    disabled={!me?.isAuthenticated}
                    style={{
                        padding: '10px 20px',
                        fontSize: '16px',
                        backgroundColor: me?.isAuthenticated ? '#28a745' : '#ccc',
                        color: 'white',
                        border: 'none',
                        borderRadius: 4,
                        cursor: me?.isAuthenticated ? 'pointer' : 'not-allowed'
                    }}
                >
                    📡 呼叫受保護 API
                </button>

                {!me?.isAuthenticated && (
                    <p style={{ color: '#666', marginTop: 8 }}>
                        ℹ️ 請先登入才能呼叫受保護的 API
                    </p>
                )}

                {secureData && (
                    <div style={{ marginTop: 16 }}>
                        <h3>API 回應資料:</h3>
                        <div style={{
                            padding: 12,
                            backgroundColor: '#d4edda',
                            border: '1px solid #c3e6cb',
                            borderRadius: 4
                        }}>
                            <p><strong>機密資料:</strong> {secureData.secret}</p>
                            <p><strong>時間戳記:</strong> {new Date(secureData.at).toLocaleString('zh-TW')}</p>
                        </div>
                        <details style={{ marginTop: 12 }}>
                            <summary style={{ cursor: 'pointer' }}>查看原始資料</summary>
                            <pre style={{
                                marginTop: 8,
                                padding: 12,
                                backgroundColor: '#fff',
                                border: '1px solid #ddd',
                                borderRadius: 4,
                                overflow: 'auto'
                            }}>
                                {JSON.stringify(secureData, null, 2)}
                            </pre>
                        </details>
                    </div>
                )}
            </section>

            {/* API 測試區域 */}
            <section style={{
                padding: 16,
                backgroundColor: '#f8f9fa',
                borderRadius: 4,
                marginTop: 24
            }}>
                <h2>API 測試區域</h2>
                <button
                    onClick={testApi}
                    style={{
                        padding: '10px 20px',
                        fontSize: '16px',
                        backgroundColor: '#007bff',
                        color: 'white',
                        border: 'none',
                        borderRadius: 4,
                        cursor: 'pointer'
                    }}
                >
                    🔄 測試 API
                </button>
            </section>
        </div>
    );
}

export default App;