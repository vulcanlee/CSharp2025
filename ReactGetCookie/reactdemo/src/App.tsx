import { useMemo, useState } from 'react'
import reactLogo from './assets/react.svg'
import viteLogo from '/vite.svg'
import './App.css'

// 定義 WeatherForecast 型別，對應後端的資料結構
interface WeatherForecast {
    Date: string;
    TemperatureC: number;
    TemperatureF: number;
    Summary: string;
}

// 簡易 Cookie 解析
function parseCookies(): Record<string, string> {
    return Object.fromEntries(
        document.cookie.split('; ').filter(Boolean).map(kv => {
            const idx = kv.indexOf('=')
            const k = kv.substring(0, idx)
            const v = kv.substring(idx + 1)
            return [k, v]
        })
    )
}

function App() {
    const today = useMemo(() => new Date().toISOString().slice(0, 10), [])
    const [date, setDate] = useState<string>(today)
    const [location, setLocation] = useState<string>('Taipei')

    const [forecasts, setForecasts] = useState<WeatherForecast[]>([])
    const [loading, setLoading] = useState(false)
    const [error, setError] = useState('')

    // 1) 以 Cookie 寫入日期與地點 (SameSite=None; Secure; Path=/; Max-Age=600)
    // 2) 呼叫 API 讓後端根據 Cookie 產生天氣預報並以 Cookie 回傳
    // 3) 從 Cookie 讀取 forecast_data，解析後渲染
    const sendCriteriaViaCookiesAndFetch = async () => {
        setLoading(true)
        setError('')
        setForecasts([])

        try {
            // 需在 HTTPS 下才能設定 Secure Cookie
            const maxAge = 600
            document.cookie = `forecast_date=${encodeURIComponent(date)}; Path=/; Max-Age=${maxAge}; SameSite=None; Secure`
            document.cookie = `forecast_location=${encodeURIComponent(location)}; Path=/; Max-Age=${maxAge}; SameSite=None; Secure`

            // 呼叫後端觸發 Set-Cookie 寫入 forecast_data
            const api = 'https://localhost:7074/WeatherForecast/CookieForecast'
            const resp = await fetch(api, {
                method: 'GET',
                credentials: 'include' // 關鍵：允許帶/收 Cookie
            })

            if (!resp.ok && resp.status !== 204) {
                throw new Error(`API 失敗: ${resp.status}`)
            }

            // 從 Cookie 取回預報
            const cookies = parseCookies()
            const json = cookies['forecast_data']
            if (!json) {
                throw new Error('未取得 forecast_data Cookie')
            }

            const foo = decodeURIComponent(json)
            const bar = JSON.parse(foo)
            const data: WeatherForecast[] = JSON.parse(decodeURIComponent(json))
            setForecasts(data)
        } catch (err) {
            setError(err instanceof Error ? err.message : '取得天氣預報時發生錯誤')
            console.error(err)
        } finally {
            setLoading(false)
        }
    }

    return (
        <>
            <div>
                <a href="https://vite.dev" target="_blank">
                    <img src={viteLogo} className="logo" alt="Vite logo" />
                </a>
                <a href="https://react.dev" target="_blank">
                    <img src={reactLogo} className="logo react" alt="React logo" />
                </a>
            </div>
            <h1>Vite + React</h1>

            {/* 條件輸入區 */}
            <div className="card" style={{ marginTop: '20px' }}>
                <h2>設定條件（以 Cookie 傳遞）</h2>
                <div style={{ display: 'flex', gap: 12, alignItems: 'center', flexWrap: 'wrap' }}>
                    <label>
                        日期：
                        <input type="date" value={date} onChange={e => setDate(e.target.value)} />
                    </label>
                    <label>
                        地點：
                        <input type="text" value={location} onChange={e => setLocation(e.target.value)} placeholder="Taipei" />
                    </label>
                    <button onClick={sendCriteriaViaCookiesAndFetch} disabled={loading}>
                        {loading ? '提交中…' : '以 Cookie 傳送並取得預報'}
                    </button>
                </div>

                {error && <p style={{ color: 'red' }}>{error}</p>}
            </div>

            {/* 天氣預報區塊 */}
            <div className="card" style={{ marginTop: '20px' }}>
                <h2>天氣預報</h2>

                {forecasts.length > 0 && (
                    <div style={{ marginTop: '20px' }}>
                        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
                            <thead>
                                <tr>
                                    <th style={{ border: '1px solid #ddd', padding: '8px' }}>日期</th>
                                    <th style={{ border: '1px solid #ddd', padding: '8px' }}>溫度 (C)</th>
                                    <th style={{ border: '1px solid #ddd', padding: '8px' }}>溫度 (F)</th>
                                    <th style={{ border: '1px solid #ddd', padding: '8px' }}>概況</th>
                                </tr>
                            </thead>
                            <tbody>
                                {forecasts.map((forecast, index) => (
                                    <tr key={index}>
                                        <td style={{ border: '1px solid #ddd,', padding: '8px' }}>{forecast.Date}</td>
                                        <td style={{ border: '1px solid #ddd', padding: '8px' }}>{forecast.TemperatureC}</td>
                                        <td style={{ border: '1px solid #ddd', padding: '8px' }}>{forecast.TemperatureF}</td>
                                        <td style={{ border: '1px solid #ddd', padding: '8px' }}>{forecast.Summary}</td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                )}
            </div>

            <p className="read-the-docs">
                Click on the Vite and React logos to learn more
            </p>
        </>
    )
}

export default App