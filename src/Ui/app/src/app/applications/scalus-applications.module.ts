import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { ScalusApplicationsComponent } from './scalus-applications.component';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { HttpClientModule } from '@angular/common/http';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatTableModule } from '@angular/material/table';
import { MatDialogModule } from '@angular/material/dialog';
import { FormsModule } from "@angular/forms";
import { EuiCoreModule } from '@elemental-ui/core';
import { ScalusApplicationsTokensDialogComponent } from './tokens/scalus-applications-tokens-dialog.component';

@NgModule({
  declarations: [
    ScalusApplicationsComponent,
    ScalusApplicationsTokensDialogComponent
  ],
  imports: [
    CommonModule,
    BrowserAnimationsModule,
    EuiCoreModule,
    HttpClientModule,
    MatFormFieldModule,
    MatSelectModule,
    MatCardModule,
    MatCheckboxModule,
    MatToolbarModule,
    MatButtonModule,
    MatInputModule,
    MatExpansionModule,
    MatIconModule,
    MatTooltipModule,
    MatTableModule,
    MatDialogModule,
    FormsModule
  ],
  providers: []
})
export class ScalusApplicationsModule { }
