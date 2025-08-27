let currentEditRowId = null;

function editRow(rowId) {
    currentEditRowId = rowId;
    const row = document.querySelector(`tr[data-row-id="${rowId}"]`);
    const formFields = document.getElementById('formFields');
    
    // Clear previous fields
    formFields.innerHTML = '';
    
    // Get column information from the table
    const columns = @Html.Raw(Json.Serialize(Model.Table.Columns.OrderBy(c => c.ColumnOrder)));
    
    columns.forEach(column => {
        const columnName = column.columnName;
        const currentValue = row.querySelector(`[data-column="${columnName}"]`)?.textContent?.trim() || '';
        
        const fieldDiv = document.createElement('div');
        fieldDiv.className = 'col-md-6 mb-3';
        
        let inputHtml = '';
        if (column.dataType.includes('date')) {
            inputHtml = `<input type="date" class="form-control" name="${columnName}" value="${currentValue}" />`;
        } else if (column.dataType.includes('decimal') || column.dataType.includes('int')) {
            inputHtml = `<input type="number" step="0.01" class="form-control" name="${columnName}" value="${currentValue}" />`;
        } else {
            inputHtml = `<input type="text" class="form-control" name="${columnName}" value="${currentValue}" />`;
        }

        fieldDiv.innerHTML = `
            <label class="form-label">${column.displayName}</label>
            ${inputHtml}
        `;
        
        formFields.appendChild(fieldDiv);
    });

    const editModal = new bootstrap.Modal(document.getElementById('editModal'));
    editModal.show();
}

function saveRow() {
    const form = document.getElementById('editForm');
    const formData = new FormData(form);
    const data = {};
    
    for (let [key, value] of formData.entries()) {
        data[key] = value;
    }

    fetch(`@Url.Action("UpdateData", "DynamicTable")?tableName=@Model.Table.TableName&rowId=${currentEditRowId}`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify(data)
    })
    .then(response => response.json())
    .then(result => {
        if (result.success) {
            // Update the table row
            const row = document.querySelector(`tr[data-row-id="${currentEditRowId}"]`);
            Object.keys(data).forEach(key => {
                const cell = row.querySelector(`[data-column="${key}"]`);
                if (cell) {
                    cell.textContent = data[key];
                }
            });

            // Show success message
            showAlert('success', result.message);
            
            // Close modal
            const editModal = bootstrap.Modal.getInstance(document.getElementById('editModal'));
            editModal.hide();
        } else {
            showAlert('danger', result.message);
        }
    })
    .catch(error => {
        console.error('Error:', error);
        showAlert('danger', 'Güncelleme sırasında hata oluştu.');
    });
}

function deleteRow(tableName, rowId) {
    document.getElementById('deleteForm').action = `@Url.Action("DeleteData", "DynamicTable")?tableName=${tableName}&rowId=${rowId}`;
    
    const deleteModal = new bootstrap.Modal(document.getElementById('deleteModal'));
    deleteModal.show();
}

function showAlert(type, message) {
    const alertDiv = document.createElement('div');
    alertDiv.className = `alert alert-${type} alert-dismissible fade show`;
    alertDiv.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;
    
    const container = document.querySelector('.container-fluid');
    container.insertBefore(alertDiv, container.firstChild);
    
    // Auto-remove after 5 seconds
    setTimeout(() => {
        alertDiv.remove();
    }, 5000);
}

function deleteTable(tableId, tableName) {
    if (confirm(`"${tableName}" tablosunu silmek istediğinizden emin misiniz? Bu işlem geri alınamaz.`)) {
        const form = document.createElement('form');
        form.method = 'POST';
        form.action = '@Url.Action("Delete", "DynamicTable")';
        
        const tableIdInput = document.createElement('input');
        tableIdInput.type = 'hidden';
        tableIdInput.name = 'id';
        tableIdInput.value = tableId;
        
        form.appendChild(tableIdInput);
        document.body.appendChild(form);
        form.submit();
    }
}

// Initialize DataTable
$(document).ready(function() {
    $('#dataTable').DataTable({
        language: {
            url: '//cdn.datatables.net/plug-ins/1.13.7/i18n/tr.json'
        },
        pageLength: @Model.PageSize,
        order: [[0, 'asc']],
        responsive: true,
        dom: 'Bfrtip',
        buttons: [
            'copy', 'csv', 'excel', 'pdf', 'print'
        ]
    });
});
