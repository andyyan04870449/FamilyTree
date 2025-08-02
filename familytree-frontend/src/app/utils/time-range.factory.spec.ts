import { TestBed } from '@angular/core/testing';
import { TimeRangeFactory } from './time-range.factory';

describe('TimeRangeFactory', () => {
  let service: TimeRangeFactory;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(TimeRangeFactory);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('Quick Time Ranges', () => {
    it('should provide predefined quick time ranges', () => {
      const ranges = service.getQuickTimeRanges();
      expect(ranges.length).toBeGreaterThan(0);
      expect(ranges.every(range => range.label && range.key && range.getValue)).toBeTruthy();
    });

    it('should create today range correctly', () => {
      const todayRange = service.getTimeRangeByKey('today');
      expect(todayRange).toBeTruthy();
      
      if (todayRange) {
        const now = new Date();
        expect(todayRange.start.getDate()).toBe(now.getDate());
        expect(todayRange.end.getDate()).toBe(now.getDate());
        expect(todayRange.start.getHours()).toBe(0);
        expect(todayRange.end.getHours()).toBe(23);
      }
    });

    it('should create last 7 days range correctly', () => {
      const range = service.createLastDaysRange(7);
      const diffDays = Math.floor((range.end.getTime() - range.start.getTime()) / (24 * 60 * 60 * 1000));
      expect(diffDays).toBe(7);
    });

    it('should filter ranges by type', () => {
      const filteredRanges = service.getQuickTimeRangesByType(['today', 'last7days']);
      expect(filteredRanges.length).toBe(2);
      expect(filteredRanges.every(range => ['today', 'last7days'].includes(range.key))).toBeTruthy();
    });
  });

  describe('Date Formatting', () => {
    it('should format date for form input correctly', () => {
      const testDate = new Date('2023-01-15T10:30:00');
      
      const dateOnly = service.formatDateForInput(testDate, { format: 'date' });
      expect(dateOnly).toBe('2023-01-15');
      
      const datetimeLocal = service.formatDateForInput(testDate, { format: 'datetime-local' });
      expect(datetimeLocal).toBe('2023-01-15T10:30');
      
      const iso = service.formatDateForInput(testDate, { format: 'iso' });
      expect(iso).toBe(testDate.toISOString());
    });

    it('should handle invalid dates gracefully', () => {
      const result = service.parseDate('invalid-date');
      expect(result).toBeNull();
    });

    it('should parse various date formats', () => {
      const isoString = '2023-01-15T10:30:00Z';
      const dateString = '2023-01-15';
      const timestamp = Date.now();
      const dateObject = new Date();

      expect(service.parseDate(isoString)).toBeInstanceOf(Date);
      expect(service.parseDate(dateString)).toBeInstanceOf(Date);
      expect(service.parseDate(timestamp)).toBeInstanceOf(Date);
      expect(service.parseDate(dateObject)).toBeInstanceOf(Date);
    });
  });

  describe('Time Range Operations', () => {
    it('should detect overlapping ranges', () => {
      const range1 = service.createCustomRange(
        new Date('2023-01-01'),
        new Date('2023-01-10')
      );
      const range2 = service.createCustomRange(
        new Date('2023-01-05'),
        new Date('2023-01-15')
      );
      const range3 = service.createCustomRange(
        new Date('2023-01-20'),
        new Date('2023-01-25')
      );

      expect(service.isRangeOverlapping(range1, range2)).toBeTruthy();
      expect(service.isRangeOverlapping(range1, range3)).toBeFalsy();
    });

    it('should merge overlapping ranges', () => {
      const ranges = [
        service.createCustomRange(new Date('2023-01-01'), new Date('2023-01-10')),
        service.createCustomRange(new Date('2023-01-05'), new Date('2023-01-15')),
        service.createCustomRange(new Date('2023-01-20'), new Date('2023-01-25'))
      ];

      const merged = service.mergeTimeRanges(ranges);
      expect(merged.length).toBe(2);
    });

    it('should calculate range duration correctly', () => {
      const range = service.createCustomRange(
        new Date('2023-01-01T00:00:00'),
        new Date('2023-01-02T00:00:00')
      );

      const duration = service.getRangeDuration(range);
      expect(duration).toBe(24 * 60 * 60 * 1000); // 24 hours in milliseconds
    });

    it('should provide meaningful duration descriptions', () => {
      const oneDayRange = service.createCustomRange(
        new Date('2023-01-01T00:00:00'),
        new Date('2023-01-02T00:00:00')
      );

      const description = service.getRangeDurationDescription(oneDayRange);
      expect(description).toContain('1 天');
    });
  });

  describe('Validation', () => {
    it('should validate time ranges correctly', () => {
      const validRange = {
        start: new Date('2023-01-01'),
        end: new Date('2023-01-10'),
        label: 'Valid Range',
        key: 'valid'
      };

      const invalidRange = {
        start: new Date('2023-01-10'),
        end: new Date('2023-01-01'),
        label: 'Invalid Range',
        key: 'invalid'
      };

      const validResult = service.validateTimeRange(validRange);
      const invalidResult = service.validateTimeRange(invalidRange);

      expect(validResult.valid).toBeTruthy();
      expect(invalidResult.valid).toBeFalsy();
      expect(invalidResult.errors.length).toBeGreaterThan(0);
    });

    it('should validate required fields', () => {
      const incompleteRange = {
        start: new Date('2023-01-01'),
        // end is missing
        label: 'Incomplete Range',
        key: 'incomplete'
      };

      const result = service.validateTimeRange(incompleteRange);
      expect(result.valid).toBeFalsy();
      expect(result.errors).toContain('結束時間不能為空');
    });

    it('should validate future dates', () => {
      const futureRange = {
        start: new Date(Date.now() + 24 * 60 * 60 * 1000), // tomorrow
        end: new Date(Date.now() + 48 * 60 * 60 * 1000), // day after tomorrow
        label: 'Future Range',
        key: 'future'
      };

      const result = service.validateTimeRange(futureRange);
      expect(result.valid).toBeFalsy();
      expect(result.errors).toContain('開始時間不能是未來時間');
    });
  });

  describe('Custom Range Creation', () => {
    it('should create custom range with auto-generated label', () => {
      const start = new Date('2023-01-01');
      const end = new Date('2023-01-10');
      
      const range = service.createCustomRange(start, end);
      expect(range.start).toEqual(start);
      expect(range.end).toEqual(end);
      expect(range.key).toBe('custom');
      expect(range.label).toBeTruthy();
    });

    it('should create custom range with provided label', () => {
      const start = new Date('2023-01-01');
      const end = new Date('2023-01-10');
      const customLabel = 'My Custom Range';
      
      const range = service.createCustomRange(start, end, customLabel);
      expect(range.label).toBe(customLabel);
    });
  });

  describe('Edge Cases', () => {
    it('should handle same start and end dates', () => {
      const sameDate = new Date('2023-01-01T12:00:00');
      const range = service.createCustomRange(sameDate, sameDate);
      
      expect(service.getRangeDuration(range)).toBe(0);
    });

    it('should handle invalid date inputs gracefully', () => {
      expect(() => service.createCustomRange(new Date('invalid'), new Date())).not.toThrow();
    });

    it('should return empty array for invalid range types', () => {
      const ranges = service.getQuickTimeRangesByType(['nonexistent']);
      expect(ranges).toEqual([]);
    });
  });
});