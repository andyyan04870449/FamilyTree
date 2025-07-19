import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AgGridModule } from 'ag-grid-angular';
import { ColDef, GridOptions } from 'ag-grid-community';
import { PersonService, Person } from '../../services/person.service';
import { AppConstants } from '../../constants/app.constants';

@Component({
  selector: 'app-person-table',
  templateUrl: './person-table.component.html',
  styleUrls: ['./person-table.component.scss'],
  standalone: true,
  imports: [CommonModule, AgGridModule]
})
export class PersonTableComponent implements OnInit {
  columnDefs: ColDef[] = [
    { headerName: 'ID', field: 'id', sortable: true, filter: true, width: 80 },
    { headerName: '姓名', field: 'name', sortable: true, filter: true, width: 120 },
    { headerName: '性別', field: 'gender', sortable: true, filter: true, width: 80 },
    { headerName: '生日', field: 'birthday', sortable: true, filter: true, width: 120 },
    { headerName: '國籍', field: 'nationality', sortable: true, filter: true, width: 100 },
    { headerName: '電話', field: 'mobile', sortable: true, filter: true, width: 150 },
    { headerName: '創建時間', field: 'createdAt', sortable: true, filter: true, width: 150,
      valueFormatter: (params) => {
        if (params.value) {
          return new Date(params.value).toLocaleString('zh-TW');
        }
        return '';
      }
    }
  ];

  rowData: Person[] = [];

  gridOptions: GridOptions = {
    pagination: true,
    paginationPageSize: 10,
    paginationPageSizeSelector: [10, 20, 50, 100],
    domLayout: 'autoHeight',
    animateRows: true,
    theme: 'legacy'
  };

  constructor(private personService: PersonService) {}

  ngOnInit() {
    this.loadPersons();
  }

  loadPersons() {
    this.personService.getPersons().subscribe({
      next: (persons) => {
        this.rowData = persons;
        console.log('Loaded persons:', persons);
      },
      error: (error) => {
        console.error('Error loading persons:', error);
        // 如果 API 失敗，使用示例數據
        this.rowData = AppConstants.SAMPLE_PERSONS;
      }
    });
  }
}
