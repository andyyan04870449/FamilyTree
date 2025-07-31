// 簡化的測試案件管理 API 的腳本
// 使用方法: node test-project-apis-simple.js

const API_BASE_URL = 'http://localhost:5088/api/project';

// 測試資料
const testProjectData = {
  projectName: `測試專案 ${new Date().toISOString()}`,
  projectDescription: '這是一個測試專案',
  userId: '123456' // 6位數字的測試用戶ID
};

let createdProjectId = null;

// 顯示結果的輔助函數
function logResult(apiName, success, data = null, error = null) {
  console.log('\n' + '='.repeat(50));
  console.log(`API: ${apiName}`);
  console.log(`狀態: ${success ? '✅ 成功' : '❌ 失敗'}`);
  if (data) {
    console.log('回應資料:', JSON.stringify(data, null, 2));
  }
  if (error) {
    console.log('錯誤訊息:', error.message || error);
  }
}

// 測試函數
async function testAPI(name, method, url, body = null) {
  try {
    const options = {
      method: method,
      headers: {
        'Content-Type': 'application/json',
      }
    };
    
    if (body) {
      options.body = JSON.stringify(body);
    }
    
    const response = await fetch(url, options);
    const data = await response.json();
    
    if (response.ok) {
      logResult(name, true, data);
      return { success: true, data };
    } else {
      logResult(name, false, null, { status: response.status, ...data });
      return { success: false, error: data };
    }
  } catch (error) {
    logResult(name, false, null, error);
    return { success: false, error };
  }
}

// 主測試流程
async function runTests() {
  console.log('開始測試案件管理 API...\n');
  console.log(`API 基礎 URL: ${API_BASE_URL}`);
  console.log(`測試時間: ${new Date().toLocaleString()}`);
  
  // 1. 測試獲取專案列表
  console.log('\n📋 1. 測試獲取專案列表');
  await testAPI('GET /api/project', 'GET', API_BASE_URL);
  
  // 2. 測試獲取專案統計
  console.log('\n📊 2. 測試獲取專案統計');
  await testAPI('GET /api/project/statistics', 'GET', `${API_BASE_URL}/statistics`);
  
  // 3. 測試建立新專案
  console.log('\n➕ 3. 測試建立新專案');
  const createResult = await testAPI('POST /api/project', 'POST', API_BASE_URL, testProjectData);
  
  // 檢查返回的數據結構
  if (createResult.success) {
    // 後端可能返回 projectId 或 project.id
    createdProjectId = createResult.data.projectId || (createResult.data.project && createResult.data.project.id);
    
    if (createdProjectId) {
      console.log(`✅ 已建立專案 ID: ${createdProjectId}`);
    
      // 4. 測試獲取單一專案
      console.log('\n🔍 4. 測試獲取單一專案');
      await testAPI('GET /api/project/{id}', 'GET', `${API_BASE_URL}/${createdProjectId}`);
      
      // 5. 測試更新專案
      console.log('\n✏️ 5. 測試更新專案');
      const updateData = {
        projectName: testProjectData.projectName + ' (已更新)',
        projectDescription: '這是更新後的描述',
        status: 'active'
      };
      await testAPI('PUT /api/project/{id}', 'PUT', `${API_BASE_URL}/${createdProjectId}`, updateData);
      
      // 6. 測試搜尋專案
      console.log('\n🔍 6. 測試搜尋專案');
      await testAPI('GET /api/project?search=測試', 'GET', `${API_BASE_URL}?search=測試`);
      
      // 7. 測試刪除專案
      console.log('\n🗑️ 7. 測試刪除專案');
      await testAPI('DELETE /api/project/{id}', 'DELETE', `${API_BASE_URL}/${createdProjectId}`);
      
    } else {
      console.log('⚠️ 無法取得專案 ID');
    }
  } else {
    console.log('⚠️ 無法建立專案，跳過後續測試');
  }
  
  // 8. 測試 API
  console.log('\n🧪 8. 測試 Test API');
  await testAPI('GET /api/project/test', 'GET', `${API_BASE_URL}/test`);
  
  console.log('\n' + '='.repeat(50));
  console.log('測試完成！');
}

// 執行測試
runTests().catch(console.error);