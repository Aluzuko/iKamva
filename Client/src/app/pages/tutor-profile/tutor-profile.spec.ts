import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { TutorProfile } from './tutor-profile';

const profile = (over: object = {}) => ({
  fullName: 'Dr. Sarah Chen', email: 's@x.com', bio: null, hourlyRate: 85, yearsExperience: 3, subjectIds: ['m'],
  verificationState: 'notSubmitted', rejectionReason: null, submittedAt: null, canEditDocuments: true,
  documents: [], missingForSubmission: ['ID document'], ...over,
});

describe('TutorProfile', () => {
  let component: TutorProfile;
  let fixture: ComponentFixture<TutorProfile>;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TutorProfile],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    fixture = TestBed.createComponent(TutorProfile);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    http.expectOne('/api/tutor/profile').flush(profile());
    http.expectOne('/api/tutor/subjects').flush([{ subjectId: 'm', name: 'Mathematics', defaultHourlyRate: 85 }]);
  });

  afterEach(() => http.verify());

  it('rejects files that are not PDF, JPG or PNG before uploading', () => {
    const input = { files: [new File(['x'], 'cv.docx', { type: 'application/msword' })], value: 'x' };
    component.onFileChosen('idDocument', { target: input } as unknown as Event);
    http.expectNone('/api/tutor/documents');
    expect(component.documentErrors().idDocument).toBe('Upload a PDF, JPG or PNG file.');
  });

  it('cannot submit while something is missing', () => {
    expect(component.canSubmit()).toBe(false);
  });

  it('asks to save profile edits before submitting', () => {
    component.profile.set(profile({ missingForSubmission: [] }) as never);
    component.form.markAsDirty();
    component.submit();
    http.expectNone('/api/tutor/verification/submit');
    expect(component.submitError()).toContain('Save your profile');
  });

  it('requires a subject before saving', () => {
    component.toggleSubject('m');
    component.saveProfile();
    http.expectNone('/api/tutor/profile');
    expect(component.invalid('subjectIds')).toBe(true);
  });
});
